using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Sync.States.Pull;
using Toggl.Foundation.Tests.Helpers;
using Toggl.Ultrawave.Exceptions;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.Pull
{
    public sealed class EnsureFetchListSucceededStateTests
    {
        [Fact, LogIfTooSlow]
        public async Task ReturnsContinueResultWhenFetchingCompletes()
        {
            var fetchObservables = createFetchObservables(Observable.Return(new List<ITestModel>()));
            var state = new EnsureFetchListSucceededState<IList<ITestModel>>();
            var transition = await state.Start(fetchObservables);

            transition.Result.Should().Be(state.Done);
        }

        [Theory, LogIfTooSlow]
        [MemberData(nameof(ApiExceptions.ServerExceptions), MemberType = typeof(ApiExceptions))]
        [MemberData(nameof(ApiExceptions.ClientExceptionsWhichAreNotReThrownInSyncStates), MemberType = typeof(ApiExceptions))]
        public async Task ReturnsFailureResultWhenFetchingThrows(ApiException exception)
        {
            var fetchObservables = createFetchObservables(Observable.Throw<List<ITestModel>>(exception));
            var state = new EnsureFetchListSucceededState<ITestModel>();
            var transition = await state.Start(fetchObservables);

            transition.Result.Should().Be(state.ErrorOccured);
        }

        [Fact, LogIfTooSlow]
        public void ThrowsIfFetchObservablePublishesTwice()
        {
            var fetchObservables = createFetchObservables(
                Observable.Create<List<ITestModel>>(observer =>
                {
                    observer.OnNext(new List<ITestModel>());
                    observer.OnNext(new List<ITestModel>());
                    return () => { };
                }));
            var state = new EnsureFetchListSucceededState<ITestModel>();
            Action fetchTwice = () => state.Start(fetchObservables).Wait();

            fetchTwice.Should().Throw<InvalidOperationException>();
        }

        [Fact, LogIfTooSlow]
        public void ThrowsWhenTheDeviceIsOffline()
        {
            var observables = createFetchObservables(Observable.Throw<List<ITestModel>>(new OfflineException(new Exception())));
            var state = new EnsureFetchListSucceededState<ITestModel>();
            Action startingState = () => state.Start(observables).Wait();

            startingState.Should().Throw<OfflineException>();
        }

        private IFetchObservables createFetchObservables(IObservable<List<ITestModel>> observable)
        {
            var observables = Substitute.For<IFetchObservables>();
            observables.GetList<ITestModel>().Returns(observable);
            return observables;
        }
    }
}
