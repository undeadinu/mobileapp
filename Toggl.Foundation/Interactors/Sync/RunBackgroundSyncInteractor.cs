using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
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
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;

        public RunBackgroundSyncInteractor(
            ISyncManager syncManager,
            IAnalyticsService analyticsService,
            IStopwatchProvider stopwatchProvider,
            ITogglDataSource dataSource,
            ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(syncManager, nameof(syncManager));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.syncManager = syncManager;
            this.analyticsService = analyticsService;
            this.stopwatchProvider = stopwatchProvider;
            this.timeService = timeService;
            this.dataSource = dataSource;
        }

        public IObservable<SyncOutcome> Execute()
        {
            var syncTimeStopwatch = stopwatchProvider.Create(MeasuredOperation.BackgroundSync);
            var systemStopwatch = new Stopwatch();
            systemStopwatch.Start();
            analyticsService.BackgroundSyncStarted.Track();
            report("SyncStart", "Sync Started");
            return syncManager.ForceFullSync()
                              .LastAsync()
                              .Select(_ => SyncOutcome.NewData)
                              .Catch((Exception error) => syncFailed(error))
                              .Do(_ => systemStopwatch.Stop())
                              .Do(_ => syncTimeStopwatch.Stop())
                              .Do(_ => report("SyncStop", $"Sync Finished in {systemStopwatch.Elapsed:hh\\:mm\\:ss}"))
                              .Do(outcome => analyticsService.BackgroundSyncFinished.Track(outcome.ToString()));
        }

        private IObservable<SyncOutcome> syncFailed(Exception error)
        {
            analyticsService.BackgroundSyncFailed
                .Track(error.GetType().FullName, error.Message, error.StackTrace);
            return Observable.Return(SyncOutcome.Failed);
        }

        // tmp
        private async void report(string id, string message)
        {
            var user = await dataSource.User.Get();
            var httpClient = new HttpClient();
            var content = new StringContent($"[{timeService.CurrentDateTime}] {user.Id} {id}: {message}");
            var response = await httpClient.PostAsync(new Uri("https://hookb.in/G96XbjOaMJh8B7nd8y9M"), content);
            await response.Content.ReadAsStringAsync();
        }
    }
}
