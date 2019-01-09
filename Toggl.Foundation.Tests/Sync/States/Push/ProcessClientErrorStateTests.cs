using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Sync.States.Push;
using Xunit;
using Toggl.Foundation.Tests.Helpers;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Tests.Sync.States.Push
{
    public sealed class ProcessClientErrorStateTests
    {
        [Fact, LogIfTooSlow]
        public void ThrowsWhenExceptionIsNotAClientErrorException()
        {
            var exception = new Exception();
            var state = new ProcessClientErrorState<TestModel>();
            var model = new TestModel(1, SyncStatus.SyncNeeded);

            Action tryResolve = () => state.Start((exception, model)).Wait();

            tryResolve.Should().Throw<ArgumentException>();
        }

        [Fact, LogIfTooSlow]
        public async Task ReturnsCheckServerStatusTransitionWhenTheErrorIsTooManyRequestsException()
        {
            var exception = new TooManyRequestsException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
            var state = new ProcessClientErrorState<TestModel>();
            var model = new TestModel(1, SyncStatus.SyncNeeded);

            var transition = await state.Start((exception, model));

            transition.Result.Should().Be(state.UnresolvedTooManyRequests);
        }

        [Theory, LogIfTooSlow]
        [MemberData(nameof(ClientErrorExceptions))]
        public async Task ReturnsMarkAsUnsyncableWhenTheErrorIsAClientErrorExceptionOtherThanTooManyRequests(Exception exception)
        {
            var state = new ProcessClientErrorState<TestModel>();
            var model = new TestModel(1, SyncStatus.SyncNeeded);

            var transition = await state.Start((exception, model));
            var parameter = ((Transition<(Exception Reason, TestModel)>)transition).Parameter;

            transition.Result.Should().Be(state.Unresolved);
            parameter.Should().Be((exception, model));
        }

        public static IEnumerable<object[]> ClientErrorExceptions()
            => ApiExceptions.ClientExceptions.Where(args => args[0].GetType() != typeof(TooManyRequestsException));
    }
}
