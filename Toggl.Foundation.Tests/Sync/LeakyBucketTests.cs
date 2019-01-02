using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Sync;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Foundation.Tests.Sync
{
    public sealed class LeakyBucketTests
    {
        public sealed class TheConstructor
        {
            [Fact]
            public void ThrowsWhenArgumentIsNull()
            {
                Action creatingLeakyBucket = () => new LeakyBucket(null, 1);

                creatingLeakyBucket.Should().Throw<ArgumentNullException>();
            }

            [Property]
            public void ThrowsWhenHorizonIsLowerOrEqualToZero(NonNegativeInt slotsPerWindow)
            {
                var timeService = Substitute.For<ITimeService>();

                Action creatingLeakyBucket = () => new LeakyBucket(timeService, -slotsPerWindow.Get);

                creatingLeakyBucket.Should().Throw<ArgumentOutOfRangeException>();
            }
        }

        public sealed class TryClaimFreeSlotMethod
        {
            private readonly DateTimeOffset baseTime
                = new DateTimeOffset(2018, 12, 1, 22, 12, 24, TimeSpan.FromHours(6));

            private readonly ITimeService timeService = Substitute.For<ITimeService>();

            [Theory]
            [InlineData(2)]
            [InlineData(10)]
            [InlineData(60)]
            public void SendsAllRequestsInAShortPeriodOfTimeUntilReachingTheLimit(int slotsPerWindow)
            {
                timeService.CurrentDateTime.Returns(baseTime);
                var bucket = new LeakyBucket(timeService, slotsPerWindow);

                for (var i = 0; i < slotsPerWindow; i++)
                {
                    var claimed = bucket.TryClaimFreeSlot(out var timeToNextFreeSlot);

                    claimed.Should().BeTrue();
                    timeToNextFreeSlot.Should().Be(TimeSpan.Zero);
                }
            }

            [Theory]
            [InlineData(2)]
            [InlineData(10)]
            [InlineData(60)]
            public void ReturnsNonZeroTimeToNextSlotTooManyRequestsAreSentInAShortPeriodOfTime(int slotsPerWindow)
            {
                timeService.CurrentDateTime.Returns(baseTime);
                var bucket = new LeakyBucket(timeService, slotsPerWindow);

                bucket.TryClaimFreeSlots(slotsPerWindow, out _);
                var claimed = bucket.TryClaimFreeSlot(out var time);

                claimed.Should().BeFalse();
                time.Should().BeGreaterThan(TimeSpan.Zero);
            }

            [Theory]
            [InlineData(2)]
            [InlineData(10)]
            [InlineData(60)]
            public void AllowsSlotsSpreadOutAcrossTheTimeLimitSoThatTheyAreNotSentTooCloseToEachOther(int slotsPerWindow)
            {
                var movingWindowSize = TimeSpan.FromSeconds(10);
                var uniformDelayBetweenRequests = movingWindowSize / slotsPerWindow;
                var times = Enumerable.Range(1, 2 * slotsPerWindow)
                    .Select(n => baseTime + (n * uniformDelayBetweenRequests)).ToArray();
                timeService.CurrentDateTime.Returns(baseTime, times);
                var bucket = new LeakyBucket(timeService, slotsPerWindow, movingWindowSize);

                for (var i = 0; i < times.Length - 1; i++)
                {
                    var claimed = bucket.TryClaimFreeSlot(out var timeToNextSlot);

                    claimed.Should().BeTrue();
                    timeToNextSlot.Should().Be(TimeSpan.Zero);
                }
            }

            [Fact]
            public void CalculatesTheDelayUntilTheNextFreeSlot()
            {
                timeService.CurrentDateTime.Returns(
                    baseTime,
                    baseTime + TimeSpan.FromSeconds(6),
                    baseTime + TimeSpan.FromSeconds(8));
                var bucket = new LeakyBucket(timeService, slotsPerWindow: 2, movingWindowSize: TimeSpan.FromSeconds(10));

                bucket.TryClaimFreeSlot(out _);
                bucket.TryClaimFreeSlot(out _);
                var claimed = bucket.TryClaimFreeSlot(out var timeToFreeSlot);

                claimed.Should().BeFalse();
                timeToFreeSlot.Should().Be(TimeSpan.FromSeconds(2));
            }
        }

        public sealed class TheTryClaimFreeSlotsMethod
        {
            private readonly DateTimeOffset baseTime
                = new DateTimeOffset(2018, 12, 1, 22, 12, 24, TimeSpan.FromHours(6));

            private readonly ITimeService timeService = Substitute.For<ITimeService>();

            [Theory]
            [InlineData(2)]
            [InlineData(10)]
            [InlineData(60)]
            public void AllowsSlotsInAShortPeriodOfTimeUntilReachingTheLimit(int slotsPerWindow)
            {
                timeService.CurrentDateTime.Returns(baseTime);
                var client = new LeakyBucket(timeService, slotsPerWindow);

                var claimed = client.TryClaimFreeSlots(slotsPerWindow, out var timeToNextFreeSlot);

                claimed.Should().BeTrue();
                timeToNextFreeSlot.Should().Be(TimeSpan.Zero);
            }

            [Theory]
            [InlineData(2)]
            [InlineData(10)]
            [InlineData(60)]
            public void ReturnsNonZeroTimeToNextSlotWhenTooManySlotsAreUsedInAShortPeriodOfTime(int slotsPerWindowLimit)
            {
                timeService.CurrentDateTime.Returns(baseTime);
                var bucket = new LeakyBucket(timeService, slotsPerWindowLimit);
                bucket.TryClaimFreeSlot(out _);

                var claimed = bucket.TryClaimFreeSlots(slotsPerWindowLimit, out var time);

                claimed.Should().BeFalse();
                time.Should().BeGreaterThan(TimeSpan.Zero);
            }

            [Fact]
            public void CalculatesTheDelayUntilNextFreeSlot()
            {
                timeService.CurrentDateTime.Returns(
                    baseTime,
                    baseTime + TimeSpan.FromSeconds(
                        3),
                    baseTime + TimeSpan.FromSeconds(6),
                    baseTime + TimeSpan.FromSeconds(8));
                var bucket = new LeakyBucket(timeService, slotsPerWindow: 4, movingWindowSize: TimeSpan.FromSeconds(10));

                bucket.TryClaimFreeSlot(out _);
                bucket.TryClaimFreeSlot(out _);
                bucket.TryClaimFreeSlot(out _);
                var claimed = bucket.TryClaimFreeSlots(3, out var timeToFreeSlot);

                claimed.Should().BeFalse();
                timeToFreeSlot.Should().Be(TimeSpan.FromSeconds(5));
            }

            [Property]
            public void ThrowsWhenTooManySlotsAreRequested(PositiveInt slotsPerWindow)
            {
                if (slotsPerWindow.Get == int.MaxValue) return;

                timeService.CurrentDateTime.Returns(baseTime);
                var bucket = new LeakyBucket(timeService, slotsPerWindow.Get);

                Action claimMany = () => bucket.TryClaimFreeSlots(slotsPerWindow.Get + 1, out _);

                claimMany.Should().Throw<InvalidOperationException>();
            }
        }
    }
}
