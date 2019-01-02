using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
    public sealed class TimeTrackedTodayInteractorTests : BaseInteractorTests
    {
        private static readonly DateTimeOffset now = new DateTimeOffset(2018, 12, 31, 1, 2, 3, TimeSpan.Zero);

        private readonly ISubject<Unit> timeEntryChanged = new Subject<Unit>();
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

        public TimeTrackedTodayInteractorTests()
        {
            DataSource.TimeEntries.Created.Returns(timeEntryChanged.Select(_ => new MockTimeEntry()));
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
            var updatedTimeEntries = timeEntries.Concat(new[] { new MockTimeEntry { Start = now, Duration = 5 } });
            DataSource.TimeEntries.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                .Returns(wherePredicateApplies(timeEntries), wherePredicateApplies(updatedTimeEntries));
            var observer = Substitute.For<IObserver<TimeSpan>>();

            interactor.Execute().Skip(1).Subscribe(observer);
            timeEntryChanged.OnNext(Unit.Default);

            observer.Received().OnNext(TimeSpan.FromSeconds(10));
        }

        [Fact, LogIfTooSlow]
        public void RecalculatesTheSumOfTheDurationOfTheTimeEntriesOnMidnight()
        {
            DataSource.TimeEntries.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                .Returns(wherePredicateApplies(timeEntries), wherePredicateApplies(timeEntries));
            var observer = Substitute.For<IObserver<TimeSpan>>();

            interactor.Execute().Skip(1).Subscribe(observer);
            midnight.OnNext(Unit.Default);

            observer.Received().OnNext(TimeSpan.FromSeconds(5));
        }

        [Fact, LogIfTooSlow]
        public void RecalculatesTheSumOfTheDurationOfTheTimeEntriesWhenThereIsSignificantTimeChange()
        {
            var updatedTimeEntries = timeEntries.Concat(new[] { new MockTimeEntry { Start = now, Duration = 5 } });
            DataSource.TimeEntries.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                .Returns(wherePredicateApplies(timeEntries), wherePredicateApplies(updatedTimeEntries));
            var observer = Substitute.For<IObserver<TimeSpan>>();

            interactor.Execute().Skip(1).Subscribe(observer);
            significantTimeChange.OnNext(Unit.Default);

            observer.Received().OnNext(TimeSpan.FromSeconds(10));
        }

        private Func<CallInfo, IObservable<IEnumerable<IThreadSafeTimeEntry>>> wherePredicateApplies(IEnumerable<IThreadSafeTimeEntry> entries)
            => callInfo => Observable.Return(
                entries.Where<IThreadSafeTimeEntry>(callInfo.Arg<Func<IDatabaseTimeEntry, bool>>()));
    }
}
