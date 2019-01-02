using System;
using System.Diagnostics;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Models;
using Toggl.Foundation.Sync;
using Toggl.Multivac;

namespace Toggl.Foundation.Interactors
{
    public class RunBackgroundSyncInteractor : IInteractor<IObservable<SyncOutcome>>
    {
        private readonly ISyncManager syncManager;
        private readonly IAnalyticsService analyticsService;
        private readonly IStopwatchProvider stopwatchProvider;

        public RunBackgroundSyncInteractor(
            ISyncManager syncManager,
            IAnalyticsService analyticsService,
            IStopwatchProvider stopwatchProvider)
        {
            Ensure.Argument.IsNotNull(syncManager, nameof(syncManager));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));

            this.syncManager = syncManager;
            this.analyticsService = analyticsService;
            this.stopwatchProvider = stopwatchProvider;
        }

        public IObservable<SyncOutcome> Execute()
        {
            var syncTimeStopwatch = stopwatchProvider.Create(MeasuredOperation.BackgroundSync);
            syncTimeStopwatch.Start();
            analyticsService.BackgroundSyncStarted.Track();
            return syncManager.ForceFullSync()
                .LastAsync()
                .Select(_ => SyncOutcome.NewData)
                .Catch((Exception error) => syncFailed(error))
                .Do(outcome =>
                {
                    syncTimeStopwatch.Stop();
                    analyticsService.BackgroundSyncFinished.Track(outcome.ToString());
                });
        }

        private IObservable<SyncOutcome> syncFailed(Exception error)
        {
            analyticsService.BackgroundSyncFailed
                .Track(error.GetType().FullName, error.Message, error.StackTrace);
            return Observable.Return(SyncOutcome.Failed);
        }
    }
}
