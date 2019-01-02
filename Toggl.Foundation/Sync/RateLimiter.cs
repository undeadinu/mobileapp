using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Toggl.Multivac;

namespace Toggl.Foundation.Sync
{
    public sealed class RateLimiter : IRateLimiter
    {
        private readonly ILeakyBucket leakyBucket;
        private readonly IScheduler scheduler;

        public RateLimiter(ILeakyBucket leakyBucket, IScheduler scheduler)
        {
            Ensure.Argument.IsNotNull(leakyBucket, nameof(leakyBucket));
            Ensure.Argument.IsNotNull(scheduler, nameof(scheduler));

            this.leakyBucket = leakyBucket;
            this.scheduler = scheduler;
        }

        public IObservable<Unit> WaitForFreeSlot()
            => leakyBucket.TryClaimFreeSlot(out var timeToFreeSlot)
                ? Observable.Return(Unit.Default)
                : Observable.Return(Unit.Default)
                    .Delay(timeToFreeSlot, scheduler)
                    .SelectMany(_ => WaitForFreeSlot());
    }
}
