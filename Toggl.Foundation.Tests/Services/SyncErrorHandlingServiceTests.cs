using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Helpers;
using Toggl.Foundation.Tests.Sync;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Foundation.Tests.Services
{
    public sealed class SyncErrorHandlingServiceTests
    {
        private readonly ISubject<Exception> errorsSubject = new Subject<Exception>();
        private readonly IErrorHandlingService errorHandlingService = Substitute.For<IErrorHandlingService>();
        private readonly ISyncManager syncManager = Substitute.For<ISyncManager>();
        private readonly ISyncErrorHandlingService syncErrorHandlingService;

        private IRequest request => Substitute.For<IRequest>();
        private IResponse response => Substitute.For<IResponse>();

        public SyncErrorHandlingServiceTests()
        {
            syncManager.Errors.Returns(errorsSubject);

            syncErrorHandlingService = new SyncErrorHandlingService(errorHandlingService);
            syncErrorHandlingService.HandleErrorsOf(syncManager);
        }

        [Fact, LogIfTooSlow]
        public void SetsTheOutdatedClientVersionFlag()
        {
            var exception = new ClientDeprecatedException(request, response);
            errorHandlingService.TryHandleDeprecationError(Arg.Any<ClientDeprecatedException>()).Returns(true);

            errorsSubject.OnNext(exception);

            errorHandlingService.Received().TryHandleDeprecationError(Arg.Is(exception));
            errorHandlingService.DidNotReceive().TryHandleUnauthorizedError(Arg.Is(exception));
        }

        [Fact, LogIfTooSlow]
        public void SetsTheOutdatedApiVersionFlag()
        {
            var exception = new ApiDeprecatedException(request, response);
            errorHandlingService.TryHandleDeprecationError(Arg.Any<ApiDeprecatedException>()).Returns(true);

            errorsSubject.OnNext(exception);

            errorHandlingService.Received().TryHandleDeprecationError(Arg.Is(exception));
            errorHandlingService.DidNotReceive().TryHandleUnauthorizedError(Arg.Is(exception));
        }

        [Fact, LogIfTooSlow]
        public void SetsTheUnauthorizedAccessFlag()
        {
            var exception = new UnauthorizedException(request, response);
            errorHandlingService.TryHandleUnauthorizedError(Arg.Any<UnauthorizedException>()).Returns(true);

            errorsSubject.OnNext(exception);

            errorHandlingService.Received().TryHandleUnauthorizedError(Arg.Is(exception));
        }

        [Theory, LogIfTooSlow]
        [MemberData(nameof(SyncManagerTests.TheProgressObservable.ExceptionsRethrownByProgressObservableOnError), MemberType = typeof(SyncManagerTests.TheProgressObservable))]
        public void DoesNotThrowForAnyExceptionWhichCanBeThrownByTheProgressObservable(Exception exception)
        {
            errorHandlingService.TryHandleUnauthorizedError(Arg.Any<UnauthorizedException>()).Returns(true);
            errorHandlingService.TryHandleDeprecationError(Arg.Any<ClientDeprecatedException>()).Returns(true);
            errorHandlingService.TryHandleDeprecationError(Arg.Any<ApiDeprecatedException>()).Returns(true);

            Action processingError = () => errorsSubject.OnNext(exception);

            processingError.Should().NotThrow();
        }

        [Theory, LogIfTooSlow]
        [MemberData(nameof(ApiExceptionsWhichAreNotThrowByTheProgressObservable))]
        public void ThrowsForDifferentException(Exception exception)
        {
            Action handling = () => errorsSubject.OnNext(exception);

            handling.Should().Throw<ArgumentException>();
        }

        public static IEnumerable<object[]> ApiExceptionsWhichAreNotThrowByTheProgressObservable()
            => ApiExceptions.ClientExceptions
                .Concat(ApiExceptions.ServerExceptions)
                .Where(args => SyncManagerTests.TheProgressObservable.ExceptionsRethrownByProgressObservableOnError()
                    .All(thrownByProgress => args[0].GetType() != thrownByProgress[0].GetType()));

    }
}
