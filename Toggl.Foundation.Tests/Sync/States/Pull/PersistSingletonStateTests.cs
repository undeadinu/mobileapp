using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Sync.States.Pull;
using Toggl.Foundation.Tests.Helpers;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Exceptions;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States
{
    public sealed class PersistSingletonStateTests
    {
        private readonly PersistSingletonState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel> state;

        private readonly ISingletonDataSource<IThreadSafeTestModel> dataSource =
            Substitute.For<ISingletonDataSource<IThreadSafeTestModel>>();

        private readonly DateTimeOffset now = new DateTimeOffset(2017, 04, 05, 12, 34, 56, TimeSpan.Zero);

        public PersistSingletonStateTests()
        {
            state = new PersistSingletonState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel>(dataSource,
                TestModel.From);
        }

        [Fact, LogIfTooSlow]
        public async Task EmitsTransitionToPersistFinished()
        {
            var entity = new TestModel(123, SyncStatus.InSync);
            var observables = createObservables(entity);

            var transition = await state.Start(observables).SingleAsync();

            transition.Result.Should().Be(state.Done);
        }

        [Fact, LogIfTooSlow]
        public void ThrowsIfFetchObservablePublishesTwice()
        {
            var fetchObservables = createFetchObservables(
                Observable.Create<ITestModel>(observer =>
                {
                    observer.OnNext(new TestModel());
                    observer.OnNext(new TestModel());
                    return () => { };
                }));

            Action fetchTwice = () => state.Start(fetchObservables).Wait();

            fetchTwice.Should().Throw<InvalidOperationException>();
        }

        [Fact, LogIfTooSlow]
        public async Task IgnoresNullValueReturnedFromTheServer()
        {
            var observables = createObservables(null);

            var transition = (Transition<IFetchObservables>)(await state.Start(observables).SingleAsync());

            transition.Result.Should().Be(state.Done);
            await dataSource.DidNotReceive().UpdateWithConflictResolution(Arg.Any<IThreadSafeTestModel>());
        }

        [Fact, LogIfTooSlow]
        public void ThrowsWhenUpdateWithConflictResolutionThrows()
        {
            var entity = new TestModel();
            var observables = createObservables(entity);
            dataSource.UpdateWithConflictResolution(Arg.Any<IThreadSafeTestModel>()).Returns(
                Observable.Throw<IConflictResolutionResult<IThreadSafeTestModel>>(new TestException()));

            Action startingState = () => state.Start(observables).SingleAsync().Wait();

            startingState.Should().Throw<TestException>();
        }

        private IFetchObservables createObservables(ITestModel entity = null)
            => createFetchObservables(Observable.Return(entity));

        private IFetchObservables createFetchObservables(IObservable<ITestModel> observable = null)
        {
            var observables = Substitute.For<IFetchObservables>();
            observables.GetSingle<ITestModel>().Returns(observable);
            return observables;
        }
    }
}
