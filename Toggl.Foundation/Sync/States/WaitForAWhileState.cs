using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Multivac;

namespace Toggl.Foundation.Sync.States
{
    public sealed class WaitForAWhileState : ISyncState<TimeSpan>
    {
        private readonly IScheduler scheduler;
        private readonly IAnalyticsService analyticsService;

        public StateResult Done { get; } = new StateResult();

        public WaitForAWhileState(IScheduler scheduler, IAnalyticsService analyticsService)
        {
            Ensure.Argument.IsNotNull(scheduler, nameof(scheduler));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));

            this.scheduler = scheduler;
            this.analyticsService = analyticsService;
        }

        public IObservable<ITransition> Start(TimeSpan delay)
            => Observable.Return(Done.Transition())
                .Do(_ => analyticsService.RateLimitingDelayDuringSyncing.Track((int)delay.TotalSeconds))
                .Delay(delay, scheduler);
    }
}
