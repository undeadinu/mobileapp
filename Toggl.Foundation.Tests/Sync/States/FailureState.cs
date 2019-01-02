using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute.ExceptionExtensions;
using Toggl.Foundation.Sync.States;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States
{
    public sealed class FailureStateTests
    {
        public sealed class TheStartMethod
        {
            private static readonly IRequest request = NSubstitute.Substitute.For<IRequest>();
            private static readonly IResponse response = NSubstitute.Substitute.For<IResponse>();

            [Theory, LogIfTooSlow]
            [MemberData(nameof(Exceptions))]
            public void ThrowsTheGivenException(Exception exception)
            {
                var state = new FailureState();

                Func<Task> start = async () => await state.Start(exception);

                start.Should().Throw<Exception>().Where(caught => caught == exception);
            }

            public static IEnumerable<object[]> Exceptions
                => new[]
                {
                    new object[] { new Exception() },
                    new object[] { new InternalServerErrorException(request, response) },
                    new object[] { new TooManyRequestsException(request, response),  }
                };
        }
    }
}
