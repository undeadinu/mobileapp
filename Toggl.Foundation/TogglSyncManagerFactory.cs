using System;
using System.Reactive.Concurrency;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Sync.States.CleanUp;
using Toggl.Foundation.Sync.States.Pull;
using Toggl.Foundation.Sync.States.Push;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.ApiClients.Interfaces;

namespace Toggl.Foundation
{
    public static class TogglSyncManager
    {
        public static ISyncManager CreateSyncManager(
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            ITimeService timeService,
            IAnalyticsService analyticsService,
            ILastTimeUsageStorage lastTimeUsageStorage,
            IScheduler scheduler)
        {
            var queue = new SyncStateQueue();
            var entryPoints = new StateMachineEntryPoints();
            var transitions = new TransitionHandlerProvider();
            ConfigureTransitions(transitions, database, api, dataSource, scheduler, timeService, analyticsService, entryPoints, queue);
            var stateMachine = new StateMachine(transitions, scheduler);
            var orchestrator = new StateMachineOrchestrator(stateMachine, entryPoints);

            return new SyncManager(queue, orchestrator, analyticsService, lastTimeUsageStorage, timeService);
        }

        public static void ConfigureTransitions(
            ITransitionConfigurator transitions,
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            IScheduler scheduler,
            ITimeService timeService,
            IAnalyticsService analyticsService,
            StateMachineEntryPoints entryPoints,
            ISyncStateQueue queue)
        {
            var minutesLeakyBucket = new LeakyBucket(timeService, slotsPerWindow: 60, movingWindowSize: TimeSpan.FromSeconds(60));
            var secondsLeakyBucket = new LeakyBucket(timeService, slotsPerWindow: 3, movingWindowSize: TimeSpan.FromSeconds(1));
            var rateLimiter = new RateLimiter(secondsLeakyBucket, scheduler);

            configurePullTransitions(transitions, database, api, dataSource, timeService, analyticsService, scheduler, entryPoints.StartPullSync, minutesLeakyBucket, rateLimiter, queue);
            configurePushTransitions(transitions, api, dataSource, analyticsService, minutesLeakyBucket, rateLimiter, scheduler, entryPoints.StartPushSync);
            configureCleanUpTransitions(transitions, timeService, dataSource, analyticsService, entryPoints.StartCleanUp);
        }

        private static void configurePullTransitions(
            ITransitionConfigurator transitions,
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            ITimeService timeService,
            IAnalyticsService analyticsService,
            IScheduler scheduler,
            StateResult entryPoint,
            ILeakyBucket leakyBucket,
            IRateLimiter rateLimiter,
            ISyncStateQueue queue)
        {
            var delayState = new DelayState(scheduler, analyticsService);

            var fetchAllSince = new FetchAllSinceState(api, database.SinceParameters, timeService, leakyBucket, rateLimiter);

            var ensureFetchWorkspacesSucceeded = new EnsureFetchListSucceededState<IWorkspace>();
            var ensureFetchWorkspaceFeaturesSucceeded = new EnsureFetchListSucceededState<IWorkspaceFeatureCollection>();
            var ensureFetchTagsSucceeded = new EnsureFetchListSucceededState<ITag>();
            var ensureFetchClientsSucceeded = new EnsureFetchListSucceededState<IClient>();
            var ensureFetchProjectsSucceeded = new EnsureFetchListSucceededState<IProject>();
            var ensureFetchTasksSucceeded = new EnsureFetchListSucceededState<ITask>();
            var ensureFetchTimeEntriesSucceeded = new EnsureFetchListSucceededState<ITimeEntry>();
            var ensureFetchUserSucceeded = new EnsureFetchSingletonSucceededState<IUser>();
            var ensureFetchPreferencesSucceeded = new EnsureFetchSingletonSucceededState<IPreferences>();

            var scheduleCleanUp = new ScheduleCleanUpState(queue);

            var detectGainingAccessToWorkspaces =
                new DetectGainingAccessToWorkspacesState(
                    dataSource.Workspaces,
                    analyticsService,
                    () => new HasFinsihedSyncBeforeInteractor(dataSource));

            var resetSinceParams = new ResetSinceParamsState(database.SinceParameters);

            var persistNewWorkspaces =
                new PersistNewWorkspacesState(dataSource.Workspaces);

            var detectLosingAccessToWorkspaces =
                new DetectLosingAccessToWorkspacesState(dataSource.Workspaces, analyticsService);

            var deleteRunningInaccessibleTimeEntry = new DeleteInaccessibleRunningTimeEntryState(dataSource.TimeEntries);

            var markWorkspacesAsInaccessible = new MarkWorkspacesAsInaccessibleState(dataSource.Workspaces);

            var persistWorkspaces =
                new PersistListState<IWorkspace, IDatabaseWorkspace, IThreadSafeWorkspace>(dataSource.Workspaces, Workspace.Clean);

            var updateWorkspacesSinceDate =
                new SinceDateUpdatingState<IWorkspace>(database.SinceParameters);

            var detectNoWorkspaceState = new NoWorkspaceDetectingState(dataSource);

            var persistWorkspaceFeatures =
                new PersistListState<IWorkspaceFeatureCollection, IDatabaseWorkspaceFeatureCollection, IThreadSafeWorkspaceFeatureCollection>(
                    dataSource.WorkspaceFeatures, WorkspaceFeatureCollection.From);

            var persistUser =
                new PersistSingletonState<IUser, IDatabaseUser, IThreadSafeUser>(dataSource.User, User.Clean);

            var noDefaultWorkspaceDetectingState = new NoDefaultWorkspaceDetectingState(dataSource, analyticsService);

            var trySetDefaultWorkspaceState = new TrySetDefaultWorkspaceState(timeService, dataSource);

            var persistTags =
                new PersistListState<ITag, IDatabaseTag, IThreadSafeTag>(dataSource.Tags, Tag.Clean);

            var updateTagsSinceDate = new SinceDateUpdatingState<ITag>(database.SinceParameters);

            var persistClients =
                new PersistListState<IClient, IDatabaseClient, IThreadSafeClient>(dataSource.Clients, Client.Clean);

            var updateClientsSinceDate = new SinceDateUpdatingState<IClient>(database.SinceParameters);

            var persistPreferences =
                new PersistSingletonState<IPreferences, IDatabasePreferences, IThreadSafePreferences>(dataSource.Preferences, Preferences.Clean);

            var persistProjects =
                new PersistListState<IProject, IDatabaseProject, IThreadSafeProject>(dataSource.Projects, Project.Clean);

            var updateProjectsSinceDate = new SinceDateUpdatingState<IProject>(database.SinceParameters);

            var createProjectPlaceholders = new CreateArchivedProjectPlaceholdersState(dataSource.Projects, analyticsService);

            var persistTimeEntries =
                new PersistListState<ITimeEntry, IDatabaseTimeEntry, IThreadSafeTimeEntry>(dataSource.TimeEntries, TimeEntry.Clean);

            var updateTimeEntriesSinceDate = new SinceDateUpdatingState<ITimeEntry>(database.SinceParameters);

            var persistTasks =
                new PersistListState<ITask, IDatabaseTask, IThreadSafeTask>(dataSource.Tasks, Task.Clean);

            var updateTasksSinceDate = new SinceDateUpdatingState<ITask>(database.SinceParameters);

            var refetchInaccessibleProjects =
                new TryFetchInaccessibleProjectsState(dataSource.Projects, timeService, api.Projects);

            // start all the API requests first
            transitions.ConfigureTransition(entryPoint, fetchAllSince);

            // prevent overloading server with too many requests
            transitions.ConfigureTransition(fetchAllSince.PreventOverloadingServer, delayState);

            // detect gaining access to workspaces
            transitions.ConfigureTransition(fetchAllSince.FetchStarted, ensureFetchWorkspacesSucceeded);
            transitions.ConfigureTransition(ensureFetchWorkspacesSucceeded.Continue, detectGainingAccessToWorkspaces);
            transitions.ConfigureTransition(detectGainingAccessToWorkspaces.Continue, detectLosingAccessToWorkspaces);
            transitions.ConfigureTransition(detectGainingAccessToWorkspaces.NewWorkspacesDetected, resetSinceParams);
            transitions.ConfigureTransition(resetSinceParams.Continue, persistNewWorkspaces);
            transitions.ConfigureTransition(persistNewWorkspaces.FinishedPersisting, fetchAllSince);

            // detect losing access to workspaces
            transitions.ConfigureTransition(detectLosingAccessToWorkspaces.Continue, persistWorkspaces);
            transitions.ConfigureTransition(detectLosingAccessToWorkspaces.WorkspaceAccessLost, markWorkspacesAsInaccessible);
            transitions.ConfigureTransition(markWorkspacesAsInaccessible.Continue, scheduleCleanUp);
            transitions.ConfigureTransition(scheduleCleanUp.CleanUpScheduled, deleteRunningInaccessibleTimeEntry);
            transitions.ConfigureTransition(deleteRunningInaccessibleTimeEntry.Continue, persistWorkspaces);

            // persist all the data pulled from the server
            transitions.ConfigureTransition(persistWorkspaces.FinishedPersisting, updateWorkspacesSinceDate);
            transitions.ConfigureTransition(updateWorkspacesSinceDate.Finished, detectNoWorkspaceState);
            transitions.ConfigureTransition(detectNoWorkspaceState.Continue, ensureFetchUserSucceeded);

            transitions.ConfigureTransition(ensureFetchUserSucceeded.Continue, persistUser);
            transitions.ConfigureTransition(persistUser.FinishedPersisting, ensureFetchWorkspaceFeaturesSucceeded);

            transitions.ConfigureTransition(ensureFetchWorkspaceFeaturesSucceeded.Continue, persistWorkspaceFeatures);
            transitions.ConfigureTransition(persistWorkspaceFeatures.FinishedPersisting, ensureFetchPreferencesSucceeded);

            transitions.ConfigureTransition(ensureFetchPreferencesSucceeded.Continue, persistPreferences);
            transitions.ConfigureTransition(persistPreferences.FinishedPersisting, ensureFetchTagsSucceeded);

            transitions.ConfigureTransition(ensureFetchTagsSucceeded.Continue, persistTags);
            transitions.ConfigureTransition(persistTags.FinishedPersisting, updateTagsSinceDate);
            transitions.ConfigureTransition(updateTagsSinceDate.Finished, ensureFetchClientsSucceeded);

            transitions.ConfigureTransition(ensureFetchClientsSucceeded.Continue, persistClients);
            transitions.ConfigureTransition(persistClients.FinishedPersisting, updateClientsSinceDate);
            transitions.ConfigureTransition(updateClientsSinceDate.Finished, ensureFetchProjectsSucceeded);

            transitions.ConfigureTransition(ensureFetchProjectsSucceeded.Continue, persistProjects);
            transitions.ConfigureTransition(persistProjects.FinishedPersisting, updateProjectsSinceDate);
            transitions.ConfigureTransition(updateProjectsSinceDate.Finished, ensureFetchTasksSucceeded);

            transitions.ConfigureTransition(ensureFetchTasksSucceeded.Continue, persistTasks);
            transitions.ConfigureTransition(persistTasks.FinishedPersisting, updateTasksSinceDate);
            transitions.ConfigureTransition(updateTasksSinceDate.Finished, ensureFetchTimeEntriesSucceeded);

            transitions.ConfigureTransition(ensureFetchTimeEntriesSucceeded.Continue, createProjectPlaceholders);
            transitions.ConfigureTransition(createProjectPlaceholders.FinishedPersisting, persistTimeEntries);
            transitions.ConfigureTransition(persistTimeEntries.FinishedPersisting, updateTimeEntriesSinceDate);
            transitions.ConfigureTransition(updateTimeEntriesSinceDate.Finished, refetchInaccessibleProjects);
            transitions.ConfigureTransition(refetchInaccessibleProjects.FetchNext, refetchInaccessibleProjects);

            transitions.ConfigureTransition(refetchInaccessibleProjects.FinishedPersisting, noDefaultWorkspaceDetectingState);
            transitions.ConfigureTransition(noDefaultWorkspaceDetectingState.NoDefaultWorkspaceDetected, trySetDefaultWorkspaceState);
            transitions.ConfigureTransition(noDefaultWorkspaceDetectingState.Continue, new DeadEndState());
            transitions.ConfigureTransition(trySetDefaultWorkspaceState.Continue, new DeadEndState());

            // fail for server errors
            transitions.ConfigureTransition(ensureFetchWorkspacesSucceeded.ErrorOccured, new FailureState());
            transitions.ConfigureTransition(ensureFetchUserSucceeded.ErrorOccured, new FailureState());
            transitions.ConfigureTransition(ensureFetchWorkspaceFeaturesSucceeded.ErrorOccured, new FailureState());
            transitions.ConfigureTransition(ensureFetchPreferencesSucceeded.ErrorOccured, new FailureState());
            transitions.ConfigureTransition(ensureFetchTagsSucceeded.ErrorOccured, new FailureState());
            transitions.ConfigureTransition(ensureFetchClientsSucceeded.ErrorOccured, new FailureState());
            transitions.ConfigureTransition(ensureFetchProjectsSucceeded.ErrorOccured, new FailureState());
            transitions.ConfigureTransition(ensureFetchTasksSucceeded.ErrorOccured, new FailureState());
            transitions.ConfigureTransition(ensureFetchTimeEntriesSucceeded.ErrorOccured, new FailureState());
            transitions.ConfigureTransition(refetchInaccessibleProjects.ErrorOccured, new FailureState());

            // delay loop
            transitions.ConfigureTransition(delayState.Continue, fetchAllSince);
        }

        private static void configurePushTransitions(
            ITransitionConfigurator transitions,
            ITogglApi api,
            ITogglDataSource dataSource,
            IAnalyticsService analyticsService,
            ILeakyBucket minutesLeakyBucket,
            IRateLimiter rateLimiter,
            IScheduler scheduler,
            StateResult entryPoint)
        {
            var delayState = new DelayState(scheduler, analyticsService);

            var pushingWorkspaces = configureCreateOnlyPush(transitions, entryPoint, dataSource.Workspaces, analyticsService, api.Workspaces, minutesLeakyBucket, rateLimiter, delayState, Workspace.Clean, Workspace.Unsyncable);
            var pushingUsers = configurePushSingleton(transitions, pushingWorkspaces.NothingToPush, dataSource.User, analyticsService, api.User, minutesLeakyBucket, rateLimiter, delayState, User.Clean, User.Unsyncable);
            var pushingPreferences = configurePushSingleton(transitions, pushingUsers.NothingToPush, dataSource.Preferences, analyticsService, api.Preferences, minutesLeakyBucket, rateLimiter, delayState, Preferences.Clean, Preferences.Unsyncable);
            var pushingTags = configureCreateOnlyPush(transitions, pushingPreferences.NothingToPush, dataSource.Tags, analyticsService, api.Tags, minutesLeakyBucket, rateLimiter, delayState, Tag.Clean, Tag.Unsyncable);
            var pushingClients = configureCreateOnlyPush(transitions, pushingTags.NothingToPush, dataSource.Clients, analyticsService, api.Clients, minutesLeakyBucket, rateLimiter, delayState, Client.Clean, Client.Unsyncable);
            var pushingProjects = configureCreateOnlyPush(transitions, pushingClients.NothingToPush, dataSource.Projects, analyticsService, api.Projects, minutesLeakyBucket, rateLimiter, delayState, Project.Clean, Project.Unsyncable);
            var pushingTimeEntries = configurePush(transitions, pushingProjects.NothingToPush, dataSource.TimeEntries, analyticsService, api.TimeEntries, api.TimeEntries, api.TimeEntries, minutesLeakyBucket, rateLimiter, delayState, TimeEntry.Clean, TimeEntry.Unsyncable);

            transitions.ConfigureTransition(delayState.Continue, pushingWorkspaces);
            transitions.ConfigureTransition(pushingTimeEntries.NothingToPush, new DeadEndState());
        }

        private static void configureCleanUpTransitions(
            ITransitionConfigurator transitions,
            ITimeService timeService,
            ITogglDataSource dataSource,
            IAnalyticsService analyticsService,
            StateResult entryPoint)
        {
            var deleteOlderEntries = new DeleteOldEntriesState(timeService, dataSource.TimeEntries);
            var deleteUnsnecessaryProjectPlaceholders = new DeleteUnnecessaryProjectPlaceholdersState(dataSource.Projects, dataSource.TimeEntries);

            var deleteInaccessibleTimeEntries = new DeleteInaccessibleTimeEntriesState(dataSource.TimeEntries);
            var deleteInaccessibleTags = new DeleteNonReferencedInaccessibleTagsState(dataSource.Tags, dataSource.TimeEntries);
            var deleteInaccessibleTasks = new DeleteNonReferencedInaccessibleTasksState(dataSource.Tasks, dataSource.TimeEntries);
            var deleteInaccessibleProjects = new DeleteNonReferencedInaccessibleProjectsState(dataSource.Projects, dataSource.Tasks, dataSource.TimeEntries);
            var deleteInaccessibleClients = new DeleteNonReferencedInaccessibleClientsState(dataSource.Clients, dataSource.Projects);
            var deleteInaccessibleWorkspaces = new DeleteNonReferencedInaccessibleWorkspacesState(
                dataSource.Workspaces,
                dataSource.TimeEntries,
                dataSource.Projects,
                dataSource.Tasks,
                dataSource.Clients,
                dataSource.Tags);

            var trackInaccesssibleDataAfterCleanUp = new TrackInaccessibleDataAfterCleanUpState(dataSource, analyticsService);
            var trackInaccesssibleWorkspacesAfterCleanUp = new TrackInaccessibleWorkspacesAfterCleanUpState(dataSource, analyticsService);

            transitions.ConfigureTransition(entryPoint, deleteOlderEntries);
            transitions.ConfigureTransition(deleteOlderEntries.FinishedDeleting, deleteUnsnecessaryProjectPlaceholders);

            transitions.ConfigureTransition(deleteUnsnecessaryProjectPlaceholders.FinishedDeleting, deleteInaccessibleTimeEntries);
            transitions.ConfigureTransition(deleteInaccessibleTimeEntries.FinishedDeleting, deleteInaccessibleTags);
            transitions.ConfigureTransition(deleteInaccessibleTags.FinishedDeleting, deleteInaccessibleTasks);
            transitions.ConfigureTransition(deleteInaccessibleTasks.FinishedDeleting, deleteInaccessibleProjects);
            transitions.ConfigureTransition(deleteInaccessibleProjects.FinishedDeleting, deleteInaccessibleClients);
            transitions.ConfigureTransition(deleteInaccessibleClients.FinishedDeleting, trackInaccesssibleDataAfterCleanUp);
            transitions.ConfigureTransition(trackInaccesssibleDataAfterCleanUp.Continue, deleteInaccessibleWorkspaces);
            transitions.ConfigureTransition(deleteInaccessibleWorkspaces.FinishedDeleting, trackInaccesssibleWorkspacesAfterCleanUp);
            transitions.ConfigureTransition(trackInaccesssibleWorkspacesAfterCleanUp.Continue, new DeadEndState());
        }

        private static PushState<TDatabase, TThreadsafe> configurePush<TModel, TDatabase, TThreadsafe>(
            ITransitionConfigurator transitions,
            IStateResult entryPoint,
            IDataSource<TThreadsafe, TDatabase> dataSource,
            IAnalyticsService analyticsService,
            ICreatingApiClient<TModel> creatingApi,
            IUpdatingApiClient<TModel> updatingApi,
            IDeletingApiClient<TModel> deletingApi,
            ILeakyBucket minutesLeakyBucket,
            IRateLimiter rateLimiter,
            DelayState delayState,
            Func<TModel, TThreadsafe> toClean,
            Func<TThreadsafe, string, TThreadsafe> toUnsyncable)
            where TModel : class, IIdentifiable, ILastChangedDatable
            where TDatabase : class, TModel, IDatabaseSyncable
            where TThreadsafe : class, TDatabase, IThreadSafeModel
        {
            var push = new PushState<TDatabase, TThreadsafe>(dataSource);
            var pushOne = new PushOneEntityState<TThreadsafe>();
            var create = new CreateEntityState<TModel, TDatabase, TThreadsafe>(creatingApi, dataSource, analyticsService, minutesLeakyBucket, rateLimiter, toClean);
            var update = new UpdateEntityState<TModel, TThreadsafe>(updatingApi, dataSource, analyticsService, minutesLeakyBucket, rateLimiter, toClean);
            var delete = new DeleteEntityState<TModel, TDatabase, TThreadsafe>(deletingApi, analyticsService, dataSource, minutesLeakyBucket, rateLimiter);
            var deleteLocal = new DeleteLocalEntityState<TDatabase, TThreadsafe>(dataSource);
            var tryResolveClientError = new TryResolveClientErrorState<TThreadsafe>();
            var unsyncable = new UnsyncableEntityState<TThreadsafe>(dataSource, toUnsyncable);

            transitions.ConfigureTransition(entryPoint, push);
            transitions.ConfigureTransition(push.PushEntity, pushOne);
            transitions.ConfigureTransition(pushOne.CreateEntity, create);
            transitions.ConfigureTransition(pushOne.UpdateEntity, update);
            transitions.ConfigureTransition(pushOne.DeleteEntity, delete);
            transitions.ConfigureTransition(pushOne.DeleteEntityLocally, deleteLocal);

            transitions.ConfigureTransition(create.ClientError, tryResolveClientError);
            transitions.ConfigureTransition(update.ClientError, tryResolveClientError);
            transitions.ConfigureTransition(delete.ClientError, tryResolveClientError);

            transitions.ConfigureTransition(create.ServerError, new FailureState());
            transitions.ConfigureTransition(update.ServerError, new FailureState());
            transitions.ConfigureTransition(delete.ServerError, new FailureState());

            transitions.ConfigureTransition(create.UnknownError, new FailureState());
            transitions.ConfigureTransition(update.UnknownError, new FailureState());
            transitions.ConfigureTransition(delete.UnknownError, new FailureState());

            transitions.ConfigureTransition(tryResolveClientError.UnresolvedTooManyRequests, new FailureState());
            transitions.ConfigureTransition(tryResolveClientError.Unresolved, unsyncable);

            transitions.ConfigureTransition(create.EntityChanged, push);
            transitions.ConfigureTransition(update.EntityChanged, push);

            transitions.ConfigureTransition(create.PreventOverloadingServer, delayState);
            transitions.ConfigureTransition(update.PreventOverloadingServer, delayState);
            transitions.ConfigureTransition(delete.PreventOverloadingServer, delayState);

            transitions.ConfigureTransition(create.Finished, push);
            transitions.ConfigureTransition(update.Finished, push);
            transitions.ConfigureTransition(delete.DeletingFinished, push);
            transitions.ConfigureTransition(deleteLocal.Deleted, push);
            transitions.ConfigureTransition(deleteLocal.DeletingFailed, push);
            transitions.ConfigureTransition(unsyncable.MarkedAsUnsyncable, push);

            return push;
        }

        private static PushState<TDatabase, TThreadsafe> configureCreateOnlyPush<TModel, TDatabase, TThreadsafe>(
            ITransitionConfigurator transitions,
            IStateResult entryPoint,
            IDataSource<TThreadsafe, TDatabase> dataSource,
            IAnalyticsService analyticsService,
            ICreatingApiClient<TModel> creatingApi,
            ILeakyBucket minutesLeakyBucket,
            IRateLimiter rateLimiter,
            DelayState delayState,
            Func<TModel, TThreadsafe> toClean,
            Func<TThreadsafe, string, TThreadsafe> toUnsyncable)
            where TModel : IIdentifiable, ILastChangedDatable
            where TDatabase : class, TModel, IDatabaseSyncable
            where TThreadsafe : class, TDatabase, IThreadSafeModel
        {
            var push = new PushState<TDatabase, TThreadsafe>(dataSource);
            var pushOne = new PushOneEntityState<TThreadsafe>();
            var create = new CreateEntityState<TModel, TDatabase, TThreadsafe>(creatingApi, dataSource, analyticsService, minutesLeakyBucket, rateLimiter, toClean);
            var tryResolveClientError = new TryResolveClientErrorState<TThreadsafe>();
            var unsyncable = new UnsyncableEntityState<TThreadsafe>(dataSource, toUnsyncable);

            transitions.ConfigureTransition(entryPoint, push);
            transitions.ConfigureTransition(push.PushEntity, pushOne);
            transitions.ConfigureTransition(pushOne.CreateEntity, create);

            transitions.ConfigureTransition(pushOne.UpdateEntity, new InvalidTransitionState($"Updating is not supported for {typeof(TModel).Name} during Push sync."));
            transitions.ConfigureTransition(pushOne.DeleteEntity, new InvalidTransitionState($"Deleting is not supported for {typeof(TModel).Name} during Push sync."));
            transitions.ConfigureTransition(pushOne.DeleteEntityLocally, new InvalidTransitionState($"Deleting locally is not supported for {typeof(TModel).Name} during Push sync."));

            transitions.ConfigureTransition(create.ClientError, tryResolveClientError);
            transitions.ConfigureTransition(create.ServerError, new FailureState());
            transitions.ConfigureTransition(create.UnknownError, new FailureState());

            transitions.ConfigureTransition(create.PreventOverloadingServer, delayState);

            transitions.ConfigureTransition(tryResolveClientError.UnresolvedTooManyRequests, new FailureState());
            transitions.ConfigureTransition(tryResolveClientError.Unresolved, unsyncable);

            transitions.ConfigureTransition(create.EntityChanged, new InvalidTransitionState($"Entity cannot have changed since updating is not supported for {typeof(TModel).Name} during Push sync."));
            transitions.ConfigureTransition(create.Finished, push);
            transitions.ConfigureTransition(unsyncable.MarkedAsUnsyncable, push);

            return push;
        }

        private static PushSingleState<TThreadsafe> configurePushSingleton<TModel, TThreadsafe>(
            ITransitionConfigurator transitions,
            IStateResult entryPoint,
            ISingletonDataSource<TThreadsafe> dataSource,
            IAnalyticsService analyticsService,
            IUpdatingApiClient<TModel> updatingApi,
            ILeakyBucket minutesLeakyBucket,
            IRateLimiter rateLimiter,
            DelayState delayState,
            Func<TModel, TThreadsafe> toClean,
            Func<TThreadsafe, string, TThreadsafe> toUnsyncable)
            where TModel : class
            where TThreadsafe : class, TModel, IThreadSafeModel, IDatabaseSyncable, IIdentifiable
        {
            var push = new PushSingleState<TThreadsafe>(dataSource);
            var pushOne = new PushOneEntityState<TThreadsafe>();
            var update = new UpdateEntityState<TModel, TThreadsafe>(updatingApi, dataSource, analyticsService, minutesLeakyBucket, rateLimiter, toClean);
            var tryResolveClientError = new TryResolveClientErrorState<TThreadsafe>();
            var unsyncable = new UnsyncableEntityState<TThreadsafe>(dataSource, toUnsyncable);

            transitions.ConfigureTransition(entryPoint, push);
            transitions.ConfigureTransition(push.PushEntity, pushOne);
            transitions.ConfigureTransition(pushOne.UpdateEntity, update);

            transitions.ConfigureTransition(pushOne.CreateEntity, new InvalidTransitionState($"Creating is not supported for {typeof(TModel).Name} during Push sync."));
            transitions.ConfigureTransition(pushOne.DeleteEntity, new InvalidTransitionState($"Deleting is not supported for {typeof(TModel).Name} during Push sync."));
            transitions.ConfigureTransition(pushOne.DeleteEntityLocally, new InvalidTransitionState($"Deleting locally is not supported for {typeof(TModel).Name} during Push sync."));

            transitions.ConfigureTransition(update.ClientError, tryResolveClientError);
            transitions.ConfigureTransition(update.ServerError, new FailureState());
            transitions.ConfigureTransition(update.UnknownError, new FailureState());

            transitions.ConfigureTransition(update.PreventOverloadingServer, delayState);

            transitions.ConfigureTransition(tryResolveClientError.UnresolvedTooManyRequests, new FailureState());
            transitions.ConfigureTransition(tryResolveClientError.Unresolved, unsyncable);

            transitions.ConfigureTransition(update.Finished, push);
            transitions.ConfigureTransition(unsyncable.MarkedAsUnsyncable, push);
            transitions.ConfigureTransition(update.EntityChanged, push);

            return push;
        }
    }
}
