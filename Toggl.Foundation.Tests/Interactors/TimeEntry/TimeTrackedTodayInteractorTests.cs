using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NSubstitute.Core;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors.TimeEntry
{
    public sealed class TimeTrackedTodayInteractorTests
    {
        private static readonly DateTimeOffset now = new DateTimeOffset(2018, 12, 31, 1, 2, 3, TimeSpan.Zero);

        public sealed class WhenThereIsNoRunningTimeEntry : BaseInteractorTests
        {
            private readonly ISubject<Unit> timeEntryChange = new Subject<Unit>();
            private readonly ISubject<Unit> midnight = new Subject<Unit>();
            private readonly ISubject<Unit> significantTimeChange = new Subject<Unit>();

            private readonly ObserveTimeTrackedTodayInteractor interactor;

            private readonly IThreadSafeTimeEntry[] timeEntries =
            {
                new MockTimeEntry { Start = now.AddDays(-1), Duration = 1 },
                new MockTimeEntry { Start = now, Duration = 2 },
                new MockTimeEntry { Start = now, Duration = 3 },
                new MockTimeEntry { Start = now.AddDays(1), Duration = 4 }
            };

            public WhenThereIsNoRunningTimeEntry()
            {
                DataSource.TimeEntries.Created.Returns(timeEntryChange.Select(_ => new MockTimeEntry()));
                DataSource.TimeEntries.Updated.Returns(Observable.Never<EntityUpdate<IThreadSafeTimeEntry>>());
                DataSource.TimeEntries.Deleted.Returns(Observable.Never<long>());
                TimeService.MidnightObservable.Returns(midnight.Select(_ => now));
                TimeService.SignificantTimeChangeObservable.Returns(significantTimeChange);
                TimeService.CurrentDateTime.Returns(now);

                interactor = new ObserveTimeTrackedTodayInteractor(TimeService, DataSource.TimeEntries);
            }

            [Fact, LogIfTooSlow]
            public async Task SumsTheDurationOfTheTimeEntriesStartedOnTheCurrentDay()
            {
                DataSource.TimeEntries.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(wherePredicateApplies(timeEntries));

                var time = await interactor.Execute().FirstAsync();

                time.TotalSeconds.Should().Be(5);
            }

            [Fact, LogIfTooSlow]
            public void RecalculatesTheSumOfTheDurationOfTheTimeEntriesStartedOnTheCurrentDayWhenTimeEntriesChange()
            {
                recalculatesOn(timeEntryChange);
            }

            [Fact, LogIfTooSlow]
            public void RecalculatesTheSumOfTheDurationOfTheTimeEntriesOnMidnight()
            {
                recalculatesOn(midnight);
            }

            [Fact, LogIfTooSlow]
            public void RecalculatesTheSumOfTheDurationOfTheTimeEntriesWhenThereIsSignificantTimeChange()
            {
                recalculatesOn(significantTimeChange);
            }

            private void recalculatesOn(IObserver<Unit> trigger)
            {
                var updatedTimeEntries = timeEntries.Concat(new[] { new MockTimeEntry { Start = now, Duration = 5 } });
                DataSource.TimeEntries.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(wherePredicateApplies(timeEntries), wherePredicateApplies(updatedTimeEntries));
                var observer = Substitute.For<IObserver<TimeSpan>>();

                interactor.Execute().Skip(1).Subscribe(observer);
                trigger.OnNext(Unit.Default);

                observer.Received().OnNext(TimeSpan.FromSeconds(10));
            }
        }

        public sealed class WhenThereIsARunningTimeEntry : BaseInteractorTests
        {
            [Fact, LogIfTooSlow]
            public async Task ReturnsATickingObservable()
            {
                var currentDateTimeSubject = new Subject<DateTimeOffset>();
                var timeService = Substitute.For<ITimeService>();
                timeService.CurrentDateTimeObservable.Returns(currentDateTimeSubject);
                timeService.CurrentDateTime.Returns(now);
                timeService.MidnightObservable.Returns(Observable.Never<DateTimeOffset>());
                timeService.SignificantTimeChangeObservable.Returns(Observable.Never<Unit>());
                currentDateTimeSubject.Subscribe(currentTime => timeService.CurrentDateTime.Returns(currentTime));
                var timeEntries = new[]
                {
                    new MockTimeEntry { Start = now.AddDays(-1), Duration = 1 },
                    new MockTimeEntry { Start = now, Duration = 2 },
                    new MockTimeEntry { Start = now, Duration = 3 },
                    new MockTimeEntry { Start = now, Duration = null },
                    new MockTimeEntry { Start = now.AddDays(1), Duration = 4 }
                };
                var timeEntriesSource = Substitute.For<ITimeEntriesSource>();
                timeEntriesSource.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(wherePredicateApplies(timeEntries));
                timeEntriesSource.Created.Returns(Observable.Never<IThreadSafeTimeEntry>());
                timeEntriesSource.Updated.Returns(Observable.Never<EntityUpdate<IThreadSafeTimeEntry>>());
                timeEntriesSource.Deleted.Returns(Observable.Never<long>());
                var observer = Substitute.For<IObserver<TimeSpan>>();

                var interactor = new ObserveTimeTrackedTodayInteractor(timeService, timeEntriesSource);
                interactor.Execute().Subscribe(observer);
                currentDateTimeSubject.OnNext(now.AddSeconds(1));
                currentDateTimeSubject.OnNext(now.AddSeconds(2));
                currentDateTimeSubject.OnNext(now.AddSeconds(3));

                Received.InOrder(() =>
                {
                    observer.OnNext(Arg.Is(TimeSpan.FromSeconds(5)));
                    observer.OnNext(Arg.Is(TimeSpan.FromSeconds(6)));
                    observer.OnNext(Arg.Is(TimeSpan.FromSeconds(7)));
                    observer.OnNext(Arg.Is(TimeSpan.FromSeconds(8)));
                });
            }
        }

        private static Func<CallInfo, IObservable<IEnumerable<IThreadSafeTimeEntry>>> wherePredicateApplies(
            IEnumerable<IThreadSafeTimeEntry> entries)
            => callInfo => Observable.Return(
                entries.Where<IThreadSafeTimeEntry>(callInfo.Arg<Func<IDatabaseTimeEntry, bool>>()));
    }
}
