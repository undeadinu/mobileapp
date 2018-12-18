using System;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class ForgotPasswordViewModelTests
    {
        public abstract class ForgotPasswordViewModelTest : BaseViewModelTests<ForgotPasswordViewModel>
        {
            protected Email ValidEmail { get; } = Email.From("person@company.com");
            protected Email InvalidEmail { get; } = Email.From("This is not an email");

            protected override ForgotPasswordViewModel CreateViewModel()
                => new ForgotPasswordViewModel(TimeService, UserAccessManager, AnalyticsService, NavigationService,
                    SchedulerProvider, RxActionFactory);
        }

        public sealed class TheConstructor : ForgotPasswordViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useTimeService,
                bool useUserAccessManager,
                bool useAnalyticsService,
                bool useNavigationService,
                bool useSchedulerProvider,
                bool useRxActionFactory)
            {
                var timeService = useTimeService ? TimeService : null;
                var userAccessManager = useUserAccessManager ? UserAccessManager : null;
                var analyticsSerivce = useAnalyticsService ? AnalyticsService : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new ForgotPasswordViewModel(
                        timeService, userAccessManager, analyticsSerivce, navigationService, schedulerProvider, rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class ThePrepareMethod : ForgotPasswordViewModelTest
        {
            [Property]
            public void SetsTheEmail(NonEmptyString emailString)
            {
                var email = Email.From(emailString.Get);

                ViewModel.Prepare(EmailParameter.With(email));

                ViewModel.Email.Value.Should().Be(email);
            }
        }

        public sealed class TheResetCommand : ForgotPasswordViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void SetsErrorMessageToEmpty()
            {
                ViewModel.Email.OnNext(ValidEmail);
                UserAccessManager
                    .ResetPassword(Arg.Any<Email>())
                    .Returns(Observable.Never<string>());

                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.ErrorMessage.Subscribe(observer);

                ViewModel.Reset.Execute();

                observer.Messages.Last().Value.Value.Should().BeEmpty();
            }

            [Fact, LogIfTooSlow]
            public void SetsPasswordResetSuccessfulToFalse()
            {
                ViewModel.Email.OnNext(ValidEmail);
                UserAccessManager
                    .ResetPassword(Arg.Any<Email>())
                    .Returns(Observable.Never<string>());

                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.PasswordResetSuccessful.Subscribe(observer);

                ViewModel.Reset.Execute();

                observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void ResetsThePassword()
            {
                ViewModel.Email.OnNext(ValidEmail);

                ViewModel.Reset.Execute();

                UserAccessManager.Received().ResetPassword(ValidEmail);
            }

            [Fact, LogIfTooSlow]
            public void TracksPasswordReset()
            {
                ViewModel.Email.OnNext(ValidEmail);

                ViewModel.Reset.Execute();

                AnalyticsService.Received().ResetPassword.Track();
            }

            [Fact, LogIfTooSlow]
            public void CannotExecuteWhenEmailIsNotValid()
            {
                ViewModel.Email.OnNext(InvalidEmail);

                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.Reset.Enabled.Subscribe(observer);

                observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void CannotExecuteWhenEmailIsEmpty()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.Reset.Enabled.Subscribe(observer);

                observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void CannotExecuteIfIsLoading()
            {
                ViewModel.Email.OnNext(ValidEmail);
                UserAccessManager
                    .ResetPassword(Arg.Any<Email>())
                    .Returns(Observable.Never<string>());

                ViewModel.Reset.Execute();

                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.Reset.Enabled.Subscribe(observer);

                observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            public sealed class WhenPasswordResetSucceeds : ForgotPasswordViewModelTest
            {
                [Fact, LogIfTooSlow]
                public void SetsIsLoadingToFalse()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    var observer = TestScheduler.CreateObserver<bool>();
                    ViewModel.Reset.Executing.Subscribe(observer);

                    ViewModel.Reset.Execute();
                    TestScheduler.Start();

                    observer.Messages.Last().Value.Value.Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public void SetsPasswordResetSuccessfulToTrue()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    var observer = TestScheduler.CreateObserver<bool>();
                    ViewModel.PasswordResetSuccessful.Subscribe(observer);

                    ViewModel.Reset.Execute();
                    TestScheduler.Start();

                    observer.Messages.Last().Value.Value.Should().BeTrue();
                }

                [Fact, LogIfTooSlow]
                public void CallsTimeServiceToCloseViewModelAfterFourSeconds()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    ViewModel.Reset.Execute();
                    TestScheduler.Start();

                    TimeService.Received().RunAfterDelay(TimeSpan.FromSeconds(4), Arg.Any<Action>());
                }

                [Fact, LogIfTooSlow]
                public void ClosesTheViewModelAfterFourSecondDelay()
                {
                    var testScheduler = new TestScheduler();
                    var timeService = new TimeService(testScheduler);
                    var viewModel = new ForgotPasswordViewModel(
                        timeService, UserAccessManager, AnalyticsService, NavigationService, SchedulerProvider, RxActionFactory);
                    viewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Return("Great success"));

                    viewModel.Reset.Execute();
                    TestScheduler.Start();
                    testScheduler.AdvanceBy(TimeSpan.FromSeconds(4).Ticks);

                    NavigationService
                        .Received()
                        .Close(
                            viewModel,
                            Arg.Is<EmailParameter>(
                                parameter => parameter.Email.Equals(ValidEmail)));
                }
            }

            public sealed class WhenPasswordResetFails : ForgotPasswordViewModelTest
            {
                [Fact, LogIfTooSlow]
                public void SetsIsLoadingToFalse()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(new Exception()));

                    var observer = TestScheduler.CreateObserver<bool>();
                    ViewModel.PasswordResetSuccessful.Subscribe(observer);

                    ViewModel.Reset.Execute();

                    observer.Messages.Last().Value.Value.Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public void SetsNoEmailErrorWhenReceivesBadRequestException()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    var exception = new BadRequestException(
                        Substitute.For<IRequest>(), Substitute.For<IResponse>());
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(exception));

                    var observer = TestScheduler.CreateObserver<string>();
                    ViewModel.ErrorMessage.Subscribe(observer);

                    ViewModel.Reset.Execute();
                    TestScheduler.Start();

                    observer.Messages.Last().Value.Value.Should().Be(Resources.PasswordResetEmailDoesNotExistError);
                }

                [Fact, LogIfTooSlow]
                public void SetsOfflineErrorWhenReceivesOfflineException()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(new OfflineException()));

                    var observer = TestScheduler.CreateObserver<string>();
                    ViewModel.ErrorMessage.Subscribe(observer);

                    ViewModel.Reset.Execute();
                    TestScheduler.Start();

                    observer.Messages.Last().Value.Value.Should().Be(Resources.PasswordResetOfflineError);
                }

                [Property]
                public void SetsApiErrorWhenReceivesApiException(NonEmptyString errorString)
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    var exception = new ApiException(
                        Substitute.For<IRequest>(),
                        Substitute.For<IResponse>(),
                        errorString.Get);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(exception));

                    var observer = TestScheduler.CreateObserver<string>();
                    ViewModel.ErrorMessage.Subscribe(observer);

                    ViewModel.Reset.Execute();
                    TestScheduler.Start();

                    observer.Messages.Last().Value.Value.Should().Be(exception.LocalizedApiErrorMessage);
                }

                [Fact, LogIfTooSlow]
                public void SetsGeneralErrorForAnyOtherException()
                {
                    ViewModel.Email.OnNext(ValidEmail);
                    UserAccessManager
                        .ResetPassword(Arg.Any<Email>())
                        .Returns(Observable.Throw<string>(new Exception()));

                    var observer = TestScheduler.CreateObserver<string>();
                    ViewModel.ErrorMessage.Subscribe(observer);

                    ViewModel.Reset.Execute();
                    TestScheduler.Start();

                    observer.Messages.Last().Value.Value.Should().Be(Resources.PasswordResetGeneralError);
                }
            }
        }

        public sealed class TheCloseCommand : ForgotPasswordViewModelTest
        {
            [Property]
            public void ClosesTheViewModelReturningTheEmail(NonEmptyString emailString)
            {
                var email = Email.From(emailString.Get);
                ViewModel.Email.OnNext(email);

                ViewModel.Close.Execute();

                TestScheduler.Start();
                NavigationService
                    .Received()
                    .Close(
                        ViewModel,
                        Arg.Is<EmailParameter>(
                            parameter => parameter.Email.Equals(email)));
            }
        }
    }
}
