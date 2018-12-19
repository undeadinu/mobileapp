using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Reports;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave;
using Toggl.Ultrawave.ApiClients;

namespace Toggl.Foundation.DataSources
{
    public sealed class TogglDataSource : ITogglDataSource
    {
        private readonly ITogglDatabase database;
        private readonly ITimeService timeService;
        private readonly IBackgroundService backgroundService;
        private readonly INotificationService notificationService;
        private readonly IErrorHandlingService errorHandlingService;
        private readonly IApplicationShortcutCreator shortcutCreator;
        private readonly TimeSpan minimumTimeInBackgroundForFullSync;

        private bool isLoggedIn;
        private IDisposable signalDisposable;
        private IDisposable midnightDisposable;
        private IDisposable errorHandlingDisposable;

        public TogglDataSource(
            ITogglApi api,
            ITogglDatabase database,
            ITimeService timeService,
            IErrorHandlingService errorHandlingService,
            IBackgroundService backgroundService,
            Func<ITogglDataSource, ISyncManager> createSyncManager,
            TimeSpan minimumTimeInBackgroundForFullSync,
            INotificationService notificationService,
            IApplicationShortcutCreator shortcutCreator,
            IAnalyticsService analyticsService)
        {
            Ensure.Argument.IsNotNull(api, nameof(api));
            Ensure.Argument.IsNotNull(database, nameof(database));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(notificationService, nameof(notificationService));
            Ensure.Argument.IsNotNull(errorHandlingService, nameof(errorHandlingService));
            Ensure.Argument.IsNotNull(backgroundService, nameof(backgroundService));
            Ensure.Argument.IsNotNull(createSyncManager, nameof(createSyncManager));
            Ensure.Argument.IsNotNull(shortcutCreator, nameof(shortcutCreator));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));

            this.database = database;
            this.timeService = timeService;
            this.shortcutCreator = shortcutCreator;
            this.backgroundService = backgroundService;
            this.notificationService = notificationService;
            this.errorHandlingService = errorHandlingService;
            this.minimumTimeInBackgroundForFullSync = minimumTimeInBackgroundForFullSync;

            User = new UserDataSource(database.User);
            Tags = new TagsDataSource(database.Tags);
            Tasks = new TasksDataSource(database.Tasks);
            Clients = new ClientsDataSource(database.Clients);
            Projects = new ProjectsDataSource(database.Projects);
            Workspaces = new WorkspacesDataSource(database.Workspaces);
            Preferences = new PreferencesDataSource(database.Preferences);
            WorkspaceFeatures = new WorkspaceFeaturesDataSource(database.WorkspaceFeatures);
            TimeEntries = new TimeEntriesDataSource(database.TimeEntries, timeService, analyticsService);

            SyncManager = createSyncManager(this);
            errorHandlingDisposable = SyncManager.Errors.Subscribe(onSyncError);

            ReportsProvider = new ReportsProvider(api, database);

            FeedbackApi = api.Feedback;

            isLoggedIn = true;
        }

        public ITimeEntriesSource TimeEntries { get; }

        public ISingletonDataSource<IThreadSafeUser> User { get; }

        public IDataSource<IThreadSafeTag, IDatabaseTag> Tags { get; }

        public IDataSource<IThreadSafeTask, IDatabaseTask> Tasks { get; }

        public IDataSource<IThreadSafeClient, IDatabaseClient> Clients { get; }

        public ISingletonDataSource<IThreadSafePreferences> Preferences { get; }

        public IDataSource<IThreadSafeProject, IDatabaseProject> Projects { get; }

        public IObservableDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> Workspaces { get; }

        public IDataSource<IThreadSafeWorkspaceFeatureCollection, IDatabaseWorkspaceFeatureCollection> WorkspaceFeatures { get; }

        public ISyncManager SyncManager { get; private set; }

        public IReportsProvider ReportsProvider { get; }

        public IFeedbackApi FeedbackApi { get; }

        public IObservable<Unit> StartSyncing()
        {
            if (isLoggedIn == false)
                throw new InvalidOperationException("Cannot start syncing after the user logged out of the app.");

            if (signalDisposable != null || midnightDisposable != null)
                throw new InvalidOperationException("The StartSyncing method has already been called.");

            signalDisposable = backgroundService.AppResumedFromBackground
                .Where(timeInBackground => timeInBackground >= minimumTimeInBackgroundForFullSync)
                .Subscribe((TimeSpan _) => SyncManager.ForceFullSync());

            midnightDisposable = timeService.MidnightObservable
                .Subscribe((DateTimeOffset _) => SyncManager.CleanUp());

            return SyncManager.ForceFullSync()
                .Select(_ => Unit.Default);
        }

        public IObservable<bool> HasUnsyncedData()
            => Observable.Merge(
                hasUnsyncedData(database.TimeEntries),
                hasUnsyncedData(database.Projects),
                hasUnsyncedData(database.User),
                hasUnsyncedData(database.Tasks),
                hasUnsyncedData(database.Clients),
                hasUnsyncedData(database.Tags),
                hasUnsyncedData(database.Workspaces))
                .Any(hasUnsynced => hasUnsynced);

        public IObservable<Unit> Logout()
            => SyncManager.Freeze()
                .FirstAsync()
                .Do(_ => isLoggedIn = false)
                .Do(stopSyncingOnSignal)
                .SelectMany(_ => database.Clear())
                .Do(shortcutCreator.OnLogout)
                .SelectMany(_ =>
                    notificationService
                        .UnscheduleAllNotifications()
                        .Catch(Observable.Return(Unit.Default)))
                .FirstAsync();

        private IObservable<bool> hasUnsyncedData<TModel>(IBaseStorage<TModel> repository)
            where TModel : IDatabaseSyncable
            => repository
                .GetAll(entity => entity.SyncStatus != SyncStatus.InSync)
                .Select(unsynced => unsynced.Any())
                .SingleAsync();

        private void onSyncError(Exception exception)
        {
            if (errorHandlingService.TryHandleDeprecationError(exception)
                || errorHandlingService.TryHandleUnauthorizedError(exception)
                || errorHandlingService.TryHandleNoWorkspaceError(exception)
                || errorHandlingService.TryHandleNoDefaultWorkspaceError(exception))
            {
                stopSyncingOnSignal();
                return;
            }

            throw new ArgumentException($"{nameof(TogglDataSource)} could not handle unknown sync error {exception.GetType().FullName}.", exception);
        }

        private void stopSyncingOnSignal()
            => signalDisposable?.Dispose();
    }
}
