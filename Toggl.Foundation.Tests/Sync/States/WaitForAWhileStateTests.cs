using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States
{
    public sealed class WaitForAWhileStateTests
    {
        public sealed class TheConstructor
        {
            [Theory]
            [ConstructorData]
            public void ThrowsWhenArgumentIsNull(bool useScheduler, bool useAnalyticsService)
            {
                var scheduler = useScheduler ? Substitute.For<IScheduler>() : null;
                var analyticsService = useAnalyticsService ? Substitute.For<IAnalyticsService>() : null;

                Action createDelayState = () => new WaitForAWhileState(scheduler, analyticsService);

                createDelayState.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheStartMethod
        {
            private readonly TestScheduler scheduler = new TestScheduler();
            private readonly IAnalyticsService analyticsService = Substitute.For<IAnalyticsService>();

            [Theory]
            [MemberData(nameof(Delays))]
            public void DoesNotContinueBeforeTheDelayIsOver(TimeSpan delay)
            {
                var observer = scheduler.CreateObserver<ITransition>();
                var state = new WaitForAWhileState(scheduler, analyticsService);

                state.Start(delay.ToPositive()).Subscribe(observer);
                scheduler.AdvanceBy(delay.ToPositive().Ticks - 10);

                observer.Messages.Should().BeEmpty();
            }

            [Theory]
            [MemberData(nameof(Delays))]
            public void CompletesWhenTheDelayIsOver(TimeSpan delay)
            {
                var observer = scheduler.CreateObserver<ITransition>();
                var state = new WaitForAWhileState(scheduler, analyticsService);

                state.Start(delay.ToPositive()).Subscribe(observer);
                scheduler.AdvanceBy(delay.ToPositive().Ticks);

                observer.Messages.Should().HaveCount(1);
            }

            [Theory]
            [MemberData(nameof(Delays))]
            public void ReturnsTheContinueTransition(TimeSpan delay)
            {
                var observer = scheduler.CreateObserver<ITransition>();
                var state = new WaitForAWhileState(scheduler, analyticsService);

                state.Start(delay.ToPositive()).Subscribe(observer);
                scheduler.AdvanceBy(delay.ToPositive().Ticks);

                observer.Messages.First().Value.Value.Result.Should().Be(state.Done);
            }

            [Theory]
            [MemberData(nameof(Delays))]
            public void TracksTheDurationOfTheDelay(TimeSpan delay)
            {
                var state = new WaitForAWhileState(scheduler, analyticsService);
                var seconds = (int)delay.TotalSeconds;

                state.Start(delay.ToPositive()).Subscribe();
                scheduler.AdvanceBy(delay.ToPositive().Ticks);

                analyticsService.RateLimitingDelayDuringSyncing.Received().Track(seconds);
            }

            public static IEnumerable<object[]> Delays
                => new[]
                {
                    new object[] { TimeSpan.FromMilliseconds(1) },
                    new object[] { TimeSpan.FromSeconds(1) },
                    new object[] { TimeSpan.FromMinutes(1) },
                    new object[] { TimeSpan.FromHours(1) },
                    new object[] { TimeSpan.FromDays(1) }
                };
        }
    }

    internal static class TimeSpanExtensions
    {
        public static TimeSpan ToPositive(this TimeSpan timeSpan)
            => timeSpan >= TimeSpan.Zero ? timeSpan : timeSpan.Negate();
    }
}
