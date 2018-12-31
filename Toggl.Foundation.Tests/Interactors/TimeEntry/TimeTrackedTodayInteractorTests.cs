using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Extensions;
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

        private readonly TimeTrackedTodayInteractor interactor;
        private readonly IThreadSafeTimeEntry[] timeEntries =
        {
            new MockTimeEntry { Start = now.AddDays(-1), Duration = 1 },
            new MockTimeEntry { Start = now, Duration = 2 },
            new MockTimeEntry { Start = now, Duration = 3 },
            new MockTimeEntry { Start = now.AddDays(1), Duration = 4 }
        };

        public TimeTrackedTodayInteractorTests()
        {
            DataSource.TimeEntries.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                .Returns(callInfo => Observable.Return(timeEntries.Where(callInfo.Arg<Func<IDatabaseTimeEntry, bool>>())));

            interactor = new TimeTrackedTodayInteractor(TimeService, DataSource.TimeEntries);
        }

        [Fact, LogIfTooSlow]
        public async Task SumsTheDurationOfTheTimeEntriesStartedOnTheCurrentDay()
        {
            var time = await interactor.Execute();

            time.TotalSeconds.Should().Be(5);
        }

        [Fact, LogIfTooSlow]
        public void RecalculatesTheSumOfTheDurationOfTheTimeEntriesStartedOnTheCurrentDayWhenTimeEntriesChange()
        {
            var subject = new Subject<Unit>();
            DataSource.TimeEntries.ItemsChanged().Returns(subject);
            DataSource.TimeEntries.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                .Returns(
                    callInfo => Observable.Return(timeEntries.Where(callInfo.Arg<Func<IDatabaseTimeEntry, bool>>())),
                    callInfo => Observable.Return(timeEntries.Where(callInfo.Arg<Func<IDatabaseTimeEntry, bool>>())));
            var observer = Substitute.For<IObserver<TimeSpan>>();

            var observable = interactor.Execute().Skip(1).Subscribe(observer);
            subject.OnNext(Unit.Default);

            observer.Received().OnNext(TimeSpan.FromSeconds(5));
        }
    }
}
