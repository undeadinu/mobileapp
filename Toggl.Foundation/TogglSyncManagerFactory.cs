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
            var delayState = new WaitForAWhileState(scheduler, analyticsService);

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
                new UpdateSinceDateState<IWorkspace>(database.SinceParameters);

            var detectNoWorkspaceState = new DetectNotHavingAccessToAnyWorkspaceState(dataSource);

            var persistWorkspaceFeatures =
                new PersistListState<IWorkspaceFeatureCollection, IDatabaseWorkspaceFeatureCollection, IThreadSafeWorkspaceFeatureCollection>(
                    dataSource.WorkspaceFeatures, WorkspaceFeatureCollection.From);

            var persistUser =
                new PersistSingletonState<IUser, IDatabaseUser, IThreadSafeUser>(dataSource.User, User.Clean);

            var noDefaultWorkspaceDetectingState = new DetectUserHavingNoDefaultWorkspaceSetState(dataSource, analyticsService);

            var trySetDefaultWorkspaceState = new TrySetDefaultWorkspaceState(timeService, dataSource);

            var persistTags =
                new PersistListState<ITag, IDatabaseTag, IThreadSafeTag>(dataSource.Tags, Tag.Clean);

            var updateTagsSinceDate = new UpdateSinceDateState<ITag>(database.SinceParameters);

            var persistClients =
                new PersistListState<IClient, IDatabaseClient, IThreadSafeClient>(dataSource.Clients, Client.Clean);

            var updateClientsSinceDate = new UpdateSinceDateState<IClient>(database.SinceParameters);

            var persistPreferences =
                new PersistSingletonState<IPreferences, IDatabasePreferences, IThreadSafePreferences>(dataSource.Preferences, Preferences.Clean);

            var persistProjects =
                new PersistListState<IProject, IDatabaseProject, IThreadSafeProject>(dataSource.Projects, Project.Clean);

            var updateProjectsSinceDate = new UpdateSinceDateState<IProject>(database.SinceParameters);

            var createProjectPlaceholders = new CreateArchivedProjectPlaceholdersState(dataSource.Projects, analyticsService);

            var persistTimeEntries =
                new PersistListState<ITimeEntry, IDatabaseTimeEntry, IThreadSafeTimeEntry>(dataSource.TimeEntries, TimeEntry.Clean);

            var updateTimeEntriesSinceDate = new UpdateSinceDateState<ITimeEntry>(database.SinceParameters);

            var persistTasks =
                new PersistListState<ITask, IDatabaseTask, IThreadSafeTask>(dataSource.Tasks, Task.Clean);

            var updateTasksSinceDate = new UpdateSinceDateState<ITask>(database.SinceParameters);

            var refetchInaccessibleProjects =
                new TryFetchInaccessibleProjectsState(dataSource.Projects, timeService, api.Projects);

            // start all the API requests first
            transitions.ConfigureTransition(entryPoint, fetchAllSince);

            // prevent overloading server with too many requests
            transitions.ConfigureTransition(fetchAllSince.PreventOverloadingServer, delayState);

            // detect gaining access to workspaces
            transitions.ConfigureTransition(fetchAllSince.Done, ensureFetchWorkspacesSucceeded);
            transitions.ConfigureTransition(ensureFetchWorkspacesSucceeded.Done, detectGainingAccessToWorkspaces);
            transitions.ConfigureTransition(detectGainingAccessToWorkspaces.Done, detectLosingAccessToWorkspaces);
            transitions.ConfigureTransition(detectGainingAccessToWorkspaces.NewWorkspacesDetected, resetSinceParams);
            transitions.ConfigureTransition(resetSinceParams.Done, persistNewWorkspaces);
            transitions.ConfigureTransition(persistNewWorkspaces.Done, fetchAllSince);

            // detect losing access to workspaces
            transitions.ConfigureTransition(detectLosingAccessToWorkspaces.Done, persistWorkspaces);
            transitions.ConfigureTransition(detectLosingAccessToWorkspaces.WorkspaceAccessLost, markWorkspacesAsInaccessible);
            transitions.ConfigureTransition(markWorkspacesAsInaccessible.Done, scheduleCleanUp);
            transitions.ConfigureTransition(scheduleCleanUp.Done, deleteRunningInaccessibleTimeEntry);
            transitions.ConfigureTransition(deleteRunningInaccessibleTimeEntry.Done, persistWorkspaces);

            // persist all the data pulled from the server
            transitions.ConfigureTransition(persistWorkspaces.Done, updateWorkspacesSinceDate);
            transitions.ConfigureTransition(updateWorkspacesSinceDate.Done, detectNoWorkspaceState);
            transitions.ConfigureTransition(detectNoWorkspaceState.Done, ensureFetchUserSucceeded);

            transitions.ConfigureTransition(ensureFetchUserSucceeded.Done, persistUser);
            transitions.ConfigureTransition(persistUser.Done, ensureFetchWorkspaceFeaturesSucceeded);

            transitions.ConfigureTransition(ensureFetchWorkspaceFeaturesSucceeded.Done, persistWorkspaceFeatures);
            transitions.ConfigureTransition(persistWorkspaceFeatures.Done, ensureFetchPreferencesSucceeded);

            transitions.ConfigureTransition(ensureFetchPreferencesSucceeded.Done, persistPreferences);
            transitions.ConfigureTransition(persistPreferences.Done, ensureFetchTagsSucceeded);

            transitions.ConfigureTransition(ensureFetchTagsSucceeded.Done, persistTags);
            transitions.ConfigureTransition(persistTags.Done, updateTagsSinceDate);
            transitions.ConfigureTransition(updateTagsSinceDate.Done, ensureFetchClientsSucceeded);

            transitions.ConfigureTransition(ensureFetchClientsSucceeded.Done, persistClients);
            transitions.ConfigureTransition(persistClients.Done, updateClientsSinceDate);
            transitions.ConfigureTransition(updateClientsSinceDate.Done, ensureFetchProjectsSucceeded);

            transitions.ConfigureTransition(ensureFetchProjectsSucceeded.Done, persistProjects);
            transitions.ConfigureTransition(persistProjects.Done, updateProjectsSinceDate);
            transitions.ConfigureTransition(updateProjectsSinceDate.Done, ensureFetchTasksSucceeded);

            transitions.ConfigureTransition(ensureFetchTasksSucceeded.Done, persistTasks);
            transitions.ConfigureTransition(persistTasks.Done, updateTasksSinceDate);
            transitions.ConfigureTransition(updateTasksSinceDate.Done, ensureFetchTimeEntriesSucceeded);

            transitions.ConfigureTransition(ensureFetchTimeEntriesSucceeded.Done, createProjectPlaceholders);
            transitions.ConfigureTransition(createProjectPlaceholders.Done, persistTimeEntries);
            transitions.ConfigureTransition(persistTimeEntries.Done, updateTimeEntriesSinceDate);
            transitions.ConfigureTransition(updateTimeEntriesSinceDate.Done, refetchInaccessibleProjects);
            transitions.ConfigureTransition(refetchInaccessibleProjects.FetchNext, refetchInaccessibleProjects);

            transitions.ConfigureTransition(refetchInaccessibleProjects.Done, noDefaultWorkspaceDetectingState);
            transitions.ConfigureTransition(noDefaultWorkspaceDetectingState.NoDefaultWorkspaceDetected, trySetDefaultWorkspaceState);
            transitions.ConfigureTransition(noDefaultWorkspaceDetectingState.Done, new DeadEndState());
            transitions.ConfigureTransition(trySetDefaultWorkspaceState.Done, new DeadEndState());

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
            transitions.ConfigureTransition(delayState.Done, fetchAllSince);
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
            var delayState = new WaitForAWhileState(scheduler, analyticsService);

            var pushingWorkspaces = configureCreateOnlyPush(transitions, entryPoint, dataSource.Workspaces, analyticsService, api.Workspaces, minutesLeakyBucket, rateLimiter, delayState, Workspace.Clean, Workspace.Unsyncable);
            var pushingUsers = configurePushSingleton(transitions, pushingWorkspaces.NoMoreChanges, dataSource.User, analyticsService, api.User, minutesLeakyBucket, rateLimiter, delayState, User.Clean, User.Unsyncable);
            var pushingPreferences = configurePushSingleton(transitions, pushingUsers.NoMoreChanges, dataSource.Preferences, analyticsService, api.Preferences, minutesLeakyBucket, rateLimiter, delayState, Preferences.Clean, Preferences.Unsyncable);
            var pushingTags = configureCreateOnlyPush(transitions, pushingPreferences.NoMoreChanges, dataSource.Tags, analyticsService, api.Tags, minutesLeakyBucket, rateLimiter, delayState, Tag.Clean, Tag.Unsyncable);
            var pushingClients = configureCreateOnlyPush(transitions, pushingTags.NoMoreChanges, dataSource.Clients, analyticsService, api.Clients, minutesLeakyBucket, rateLimiter, delayState, Client.Clean, Client.Unsyncable);
            var pushingProjects = configureCreateOnlyPush(transitions, pushingClients.NoMoreChanges, dataSource.Projects, analyticsService, api.Projects, minutesLeakyBucket, rateLimiter, delayState, Project.Clean, Project.Unsyncable);
            var pushingTimeEntries = configurePush(transitions, pushingProjects.NoMoreChanges, dataSource.TimeEntries, analyticsService, api.TimeEntries, api.TimeEntries, api.TimeEntries, minutesLeakyBucket, rateLimiter, delayState, TimeEntry.Clean, TimeEntry.Unsyncable);

            transitions.ConfigureTransition(delayState.Done, pushingWorkspaces);
            transitions.ConfigureTransition(pushingTimeEntries.NoMoreChanges, new DeadEndState());
        }

        private static void configureCleanUpTransitions(
            ITransitionConfigurator transitions,
            ITimeService timeService,
            ITogglDataSource dataSource,
            IAnalyticsService analyticsService,
            StateResult entryPoint)
        {
            var deleteOlderEntries = new DeleteOldTimeEntriesState(timeService, dataSource.TimeEntries);
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
            transitions.ConfigureTransition(deleteOlderEntries.Done, deleteUnsnecessaryProjectPlaceholders);

            transitions.ConfigureTransition(deleteUnsnecessaryProjectPlaceholders.Done, deleteInaccessibleTimeEntries);
            transitions.ConfigureTransition(deleteInaccessibleTimeEntries.Done, deleteInaccessibleTags);
            transitions.ConfigureTransition(deleteInaccessibleTags.Done, deleteInaccessibleTasks);
            transitions.ConfigureTransition(deleteInaccessibleTasks.Done, deleteInaccessibleProjects);
            transitions.ConfigureTransition(deleteInaccessibleProjects.Done, deleteInaccessibleClients);
            transitions.ConfigureTransition(deleteInaccessibleClients.Done, trackInaccesssibleDataAfterCleanUp);
            transitions.ConfigureTransition(trackInaccesssibleDataAfterCleanUp.Done, deleteInaccessibleWorkspaces);
            transitions.ConfigureTransition(deleteInaccessibleWorkspaces.Done, trackInaccesssibleWorkspacesAfterCleanUp);
            transitions.ConfigureTransition(trackInaccesssibleWorkspacesAfterCleanUp.Done, new DeadEndState());
        }

        private static LookForChangeToPushState<TDatabase, TThreadsafe> configurePush<TModel, TDatabase, TThreadsafe>(
            ITransitionConfigurator transitions,
            IStateResult entryPoint,
            IDataSource<TThreadsafe, TDatabase> dataSource,
            IAnalyticsService analyticsService,
            ICreatingApiClient<TModel> creatingApi,
            IUpdatingApiClient<TModel> updatingApi,
            IDeletingApiClient<TModel> deletingApi,
            ILeakyBucket minutesLeakyBucket,
            IRateLimiter rateLimiter,
            WaitForAWhileState waitForAWhileState,
            Func<TModel, TThreadsafe> toClean,
            Func<TThreadsafe, string, TThreadsafe> toUnsyncable)
            where TModel : class, IIdentifiable, ILastChangedDatable
            where TDatabase : class, TModel, IDatabaseSyncable
            where TThreadsafe : class, TDatabase, IThreadSafeModel
        {
            var lookForChange = new LookForChangeToPushState<TDatabase, TThreadsafe>(dataSource);
            var chooseOperation = new ChooseSyncOperationState<TThreadsafe>();
            var create = new CreateEntityState<TModel, TDatabase, TThreadsafe>(creatingApi, dataSource, analyticsService, minutesLeakyBucket, rateLimiter, toClean);
            var update = new UpdateEntityState<TModel, TThreadsafe>(updatingApi, dataSource, analyticsService, minutesLeakyBucket, rateLimiter, toClean);
            var delete = new DeleteEntityState<TModel, TDatabase, TThreadsafe>(deletingApi, analyticsService, dataSource, minutesLeakyBucket, rateLimiter);
            var deleteLocal = new DeleteLocalEntityState<TDatabase, TThreadsafe>(dataSource);
            var processClientError = new ProcessClientErrorState<TThreadsafe>();
            var unsyncable = new MarkEntityAsUnsyncableState<TThreadsafe>(dataSource, toUnsyncable);

            transitions.ConfigureTransition(entryPoint, lookForChange);
            transitions.ConfigureTransition(lookForChange.ChangeFound, chooseOperation);
            transitions.ConfigureTransition(chooseOperation.CreateEntity, create);
            transitions.ConfigureTransition(chooseOperation.UpdateEntity, update);
            transitions.ConfigureTransition(chooseOperation.DeleteEntity, delete);
            transitions.ConfigureTransition(chooseOperation.DeleteEntityLocally, deleteLocal);

            transitions.ConfigureTransition(create.ClientError, processClientError);
            transitions.ConfigureTransition(update.ClientError, processClientError);
            transitions.ConfigureTransition(delete.ClientError, processClientError);

            transitions.ConfigureTransition(create.ServerError, new FailureState());
            transitions.ConfigureTransition(update.ServerError, new FailureState());
            transitions.ConfigureTransition(delete.ServerError, new FailureState());

            transitions.ConfigureTransition(create.UnknownError, new FailureState());
            transitions.ConfigureTransition(update.UnknownError, new FailureState());
            transitions.ConfigureTransition(delete.UnknownError, new FailureState());

            transitions.ConfigureTransition(processClientError.UnresolvedTooManyRequests, new FailureState());
            transitions.ConfigureTransition(processClientError.Unresolved, unsyncable);

            transitions.ConfigureTransition(create.EntityChanged, lookForChange);
            transitions.ConfigureTransition(update.EntityChanged, lookForChange);

            transitions.ConfigureTransition(create.PreventOverloadingServer, waitForAWhileState);
            transitions.ConfigureTransition(update.PreventOverloadingServer, waitForAWhileState);
            transitions.ConfigureTransition(delete.PreventOverloadingServer, waitForAWhileState);

            transitions.ConfigureTransition(create.Done, lookForChange);
            transitions.ConfigureTransition(update.Done, lookForChange);
            transitions.ConfigureTransition(delete.Done, lookForChange);
            transitions.ConfigureTransition(deleteLocal.Done, lookForChange);
            transitions.ConfigureTransition(deleteLocal.DeletingFailed, lookForChange);
            transitions.ConfigureTransition(unsyncable.Done, lookForChange);

            return lookForChange;
        }

        private static LookForChangeToPushState<TDatabase, TThreadsafe> configureCreateOnlyPush<TModel, TDatabase, TThreadsafe>(
            ITransitionConfigurator transitions,
            IStateResult entryPoint,
            IDataSource<TThreadsafe, TDatabase> dataSource,
            IAnalyticsService analyticsService,
            ICreatingApiClient<TModel> creatingApi,
            ILeakyBucket minutesLeakyBucket,
            IRateLimiter rateLimiter,
            WaitForAWhileState waitForAWhileState,
            Func<TModel, TThreadsafe> toClean,
            Func<TThreadsafe, string, TThreadsafe> toUnsyncable)
            where TModel : IIdentifiable, ILastChangedDatable
            where TDatabase : class, TModel, IDatabaseSyncable
            where TThreadsafe : class, TDatabase, IThreadSafeModel
        {
            var lookForChange = new LookForChangeToPushState<TDatabase, TThreadsafe>(dataSource);
            var chooseOperation = new ChooseSyncOperationState<TThreadsafe>();
            var create = new CreateEntityState<TModel, TDatabase, TThreadsafe>(creatingApi, dataSource, analyticsService, minutesLeakyBucket, rateLimiter, toClean);
            var processClientError = new ProcessClientErrorState<TThreadsafe>();
            var unsyncable = new MarkEntityAsUnsyncableState<TThreadsafe>(dataSource, toUnsyncable);

            transitions.ConfigureTransition(entryPoint, lookForChange);
            transitions.ConfigureTransition(lookForChange.ChangeFound, chooseOperation);
            transitions.ConfigureTransition(chooseOperation.CreateEntity, create);

            transitions.ConfigureTransition(chooseOperation.UpdateEntity, new InvalidTransitionState($"Updating is not supported for {typeof(TModel).Name} during Push sync."));
            transitions.ConfigureTransition(chooseOperation.DeleteEntity, new InvalidTransitionState($"Deleting is not supported for {typeof(TModel).Name} during Push sync."));
            transitions.ConfigureTransition(chooseOperation.DeleteEntityLocally, new InvalidTransitionState($"Deleting locally is not supported for {typeof(TModel).Name} during Push sync."));

            transitions.ConfigureTransition(create.ClientError, processClientError);
            transitions.ConfigureTransition(create.ServerError, new FailureState());
            transitions.ConfigureTransition(create.UnknownError, new FailureState());

            transitions.ConfigureTransition(create.PreventOverloadingServer, waitForAWhileState);

            transitions.ConfigureTransition(processClientError.UnresolvedTooManyRequests, new FailureState());
            transitions.ConfigureTransition(processClientError.Unresolved, unsyncable);

            transitions.ConfigureTransition(create.EntityChanged, new InvalidTransitionState($"Entity cannot have changed since updating is not supported for {typeof(TModel).Name} during Push sync."));
            transitions.ConfigureTransition(create.Done, lookForChange);
            transitions.ConfigureTransition(unsyncable.Done, lookForChange);

            return lookForChange;
        }

        private static LookForSingletonChangeToPushState<TThreadsafe> configurePushSingleton<TModel, TThreadsafe>(
            ITransitionConfigurator transitions,
            IStateResult entryPoint,
            ISingletonDataSource<TThreadsafe> dataSource,
            IAnalyticsService analyticsService,
            IUpdatingApiClient<TModel> updatingApi,
            ILeakyBucket minutesLeakyBucket,
            IRateLimiter rateLimiter,
            WaitForAWhileState waitForAWhileState,
            Func<TModel, TThreadsafe> toClean,
            Func<TThreadsafe, string, TThreadsafe> toUnsyncable)
            where TModel : class
            where TThreadsafe : class, TModel, IThreadSafeModel, IDatabaseSyncable, IIdentifiable
        {
            var lookForChange = new LookForSingletonChangeToPushState<TThreadsafe>(dataSource);
            var chooseOperation = new ChooseSyncOperationState<TThreadsafe>();
            var update = new UpdateEntityState<TModel, TThreadsafe>(updatingApi, dataSource, analyticsService, minutesLeakyBucket, rateLimiter, toClean);
            var processClientError = new ProcessClientErrorState<TThreadsafe>();
            var unsyncable = new MarkEntityAsUnsyncableState<TThreadsafe>(dataSource, toUnsyncable);

            transitions.ConfigureTransition(entryPoint, lookForChange);
            transitions.ConfigureTransition(lookForChange.ChangeFound, chooseOperation);
            transitions.ConfigureTransition(chooseOperation.UpdateEntity, update);

            transitions.ConfigureTransition(chooseOperation.CreateEntity, new InvalidTransitionState($"Creating is not supported for {typeof(TModel).Name} during Push sync."));
            transitions.ConfigureTransition(chooseOperation.DeleteEntity, new InvalidTransitionState($"Deleting is not supported for {typeof(TModel).Name} during Push sync."));
            transitions.ConfigureTransition(chooseOperation.DeleteEntityLocally, new InvalidTransitionState($"Deleting locally is not supported for {typeof(TModel).Name} during Push sync."));

            transitions.ConfigureTransition(update.ClientError, processClientError);
            transitions.ConfigureTransition(update.ServerError, new FailureState());
            transitions.ConfigureTransition(update.UnknownError, new FailureState());

            transitions.ConfigureTransition(update.PreventOverloadingServer, waitForAWhileState);

            transitions.ConfigureTransition(processClientError.UnresolvedTooManyRequests, new FailureState());
            transitions.ConfigureTransition(processClientError.Unresolved, unsyncable);

            transitions.ConfigureTransition(update.Done, lookForChange);
            transitions.ConfigureTransition(unsyncable.Done, lookForChange);
            transitions.ConfigureTransition(update.EntityChanged, lookForChange);

            return lookForChange;
        }
    }
}
