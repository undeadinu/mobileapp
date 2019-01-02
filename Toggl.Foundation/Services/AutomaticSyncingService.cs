using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Login;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Services
{
    public sealed class AutomaticSyncingService : IAutomaticSyncingService
    {
        public static TimeSpan MinimumTimeInBackgroundForFullSync { get; } = TimeSpan.FromMinutes(5);

        private readonly IBackgroundService backgroundService;
        private readonly ITimeService timeService;
        private readonly IAnalyticsService analyticsService;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        private CompositeDisposable syncingDisposeBag;

        public AutomaticSyncingService(
            IBackgroundService backgroundService,
            ITimeService timeService,
            IAnalyticsService analyticsService)
        {
            Ensure.Argument.IsNotNull(backgroundService, nameof(backgroundService));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));

            this.backgroundService = backgroundService;
            this.timeService = timeService;
            this.analyticsService = analyticsService;
        }

        public void SetupAutomaticSync(IUserAccessManager userAccessManager)
        {
            userAccessManager.UserLoggedIn
                .Subscribe(start)
                .DisposedBy(disposeBag);

            userAccessManager.UserLoggedOut
                .Subscribe(_ => stop())
                .DisposedBy(disposeBag);
        }

        private void start(ITogglDataSource dataSource)
        {
            stop();

            backgroundService.AppResumedFromBackground
                .Where(timeInBackground => timeInBackground >= MinimumTimeInBackgroundForFullSync)
                .TakeUntil(dataSource.SyncManager.Errors)
                .Subscribe(_ => sync(dataSource.SyncManager, dataSource.TimeEntries))
                .DisposedBy(syncingDisposeBag);

            timeService.MidnightObservable
                .TakeUntil(dataSource.SyncManager.Errors)
                .Subscribe(_ => dataSource.SyncManager.CleanUp())
                .DisposedBy(syncingDisposeBag);
        }

        private void stop()
        {
            syncingDisposeBag?.Dispose();
            syncingDisposeBag = new CompositeDisposable();
        }

        private async void sync(ISyncManager syncManager, ITimeEntriesSource timeEntries)
        {
            var numberOfSyncedTimeEntries = 0;

            // To ignore time entries created/updated in the app it is enough to filter out all the ones which need sync.
            // There is still a problem with the entities which are created in the app and are immediately pushed
            // during the initial sync. I think it's OK to include these for the time being and re-evaluate this
            // decision once we collect some data.

            var subscription =
                Observable.Merge(
                        timeEntries.Created.Where(timeEntry => timeEntry.SyncStatus == SyncStatus.InSync).SelectUnit(),
                        timeEntries.Updated.Where(update => update.Entity.SyncStatus == SyncStatus.InSync).SelectUnit(),
                        timeEntries.Deleted.SelectUnit())
                    .Subscribe(_ => numberOfSyncedTimeEntries++);

            await syncManager.ForceFullSync();
            subscription.Dispose();

            analyticsService.NumberOfSyncedTimeEntriesWhenResumingTheAppFromBackground.Track(numberOfSyncedTimeEntries);
        }
    }
}
