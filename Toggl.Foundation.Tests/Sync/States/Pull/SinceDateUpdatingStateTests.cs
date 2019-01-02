using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Sync.States.Pull;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Exceptions;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States
{
    public sealed class SinceDateUpdatingStateTests
    {
        private readonly ISinceParameterRepository sinceParameterRepository =
            Substitute.For<ISinceParameterRepository>();

        private readonly SinceDateUpdatingState<ITestModel> state;

        private readonly DateTimeOffset now = new DateTimeOffset(2017, 04, 05, 12, 34, 56, TimeSpan.Zero);

        private readonly DateTimeOffset at = new DateTimeOffset(2017, 09, 01, 12, 34, 56, TimeSpan.Zero);

        public SinceDateUpdatingStateTests()
        {
            state = new SinceDateUpdatingState<ITestModel>(sinceParameterRepository);
        }

        [Fact]
        public async Task ReturnsSuccessResultWhenEverythingWorks()
        {
            var fetchObservables = createObservables(new List<ITestModel>());

            var transition = await state.Start(fetchObservables);

            transition.Result.Should().Be(state.Finished);
        }

        [Fact, LogIfTooSlow]
        public async Task DoesNotUpdateSinceParametersWhenNothingIsFetched()
        {
            var observables = createObservables(new List<ITestModel>());

            await state.Start(observables).SingleAsync();

            sinceParameterRepository.DidNotReceive().Set<IDatabaseTestModel>(Arg.Any<DateTimeOffset?>());
        }

        [Fact, LogIfTooSlow]
        public async Task UpdatesSinceParametersOfTheFetchedEntity()
        {
            var newAt = new DateTimeOffset(2017, 10, 01, 12, 34, 56, TimeSpan.Zero);
            var entities = new List<ITestModel> { new TestModel { At = newAt } };
            var observables = createObservables(entities);
            sinceParameterRepository.Supports<IDatabaseTestModel>().Returns(true);

            await state.Start(observables).SingleAsync();

            sinceParameterRepository.Received().Set<IDatabaseTestModel>(Arg.Is(newAt));
        }

        [Fact, LogIfTooSlow]
        public async Task SelectsTheLatestAtValue()
        {
            var entities = creteEntitiesList(at);
            var observables = createObservables(entities);
            sinceParameterRepository.Supports<IDatabaseTestModel>().Returns(true);

            await state.Start(observables).SingleAsync();

            sinceParameterRepository.Received().Set<IDatabaseTestModel>(Arg.Is<DateTimeOffset?>(at));
        }

        private IFetchObservables createObservables(List<ITestModel> entities = null)
        {
            var observables = Substitute.For<IFetchObservables>();
            var observable = Observable.Return(entities).ConnectedReplay();
            observables.GetList<ITestModel>().Returns(observable);
            return observables;
        }

        private List<ITestModel> creteEntitiesList(DateTimeOffset? maybeAt)
        {
            var at = maybeAt ?? now;
            return new List<ITestModel>
            {
                new TestModel { At = at.AddDays(-1), Id = 0 },
                new TestModel { At = at.AddDays(-3), Id = 1 },
                new TestModel { At = at, Id = 2, ServerDeletedAt = at.AddDays(-1) },
                new TestModel { At = at.AddDays(-2), Id = 3 }
            };
        }
    }
}
