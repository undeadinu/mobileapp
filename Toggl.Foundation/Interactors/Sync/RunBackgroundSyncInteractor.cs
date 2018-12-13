using System;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Multivac;
using Toggl.Foundation.Models;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Diagnostics;

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
            analyticsService.BackgroundSyncStarted.Track();
            return syncManager.ForceFullSync()
                              .LastAsync()
                              .Select(_ => SyncOutcome.NewData)
                              .Catch((Exception error) => syncFailed(error))
                              .Do(_ => syncTimeStopwatch.Stop())
                              .Do(outcome => analyticsService.BackgroundSyncFinished.Track(outcome.ToString()));
        }

        private IObservable<SyncOutcome> syncFailed(Exception error)
        {
            analyticsService.BackgroundSyncFailed
                .Track(error.GetType().FullName, error.Message, error.StackTrace);
            return Observable.Return(SyncOutcome.Failed);
        }
    }
}
