using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using NUnit.Framework;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;
using static Toggl.Foundation.MvvmCross.ViewModels.SignupViewModel;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SignupViewModelTests
    {
        public abstract class SignupViewModelTest : BaseViewModelTests<SignupViewModel>
        {
            protected Email ValidEmail { get; } = Email.From("susancalvin@psychohistorian.museum");
            protected Email InvalidEmail { get; } = Email.From("foo@");

            protected Password ValidPassword { get; } = Password.From("123456");
            protected Password InvalidPassword { get; } = Password.Empty;

            protected ILocation Location { get; } = Substitute.For<ILocation>();
            protected ILastTimeUsageStorage LastTimeUsageStorage { get; } = Substitute.For<ILastTimeUsageStorage>();

            protected override SignupViewModel CreateViewModel()
                => new SignupViewModel(
                    ApiFactory,
                    UserAccessManager,
                    AnalyticsService,
                    OnboardingStorage,
                    NavigationService,
                    ErrorHandlingService,
                    LastTimeUsageStorage,
                    TimeService,
                    SchedulerProvider,
                    RxActionFactory);

            protected override void AdditionalSetup()
            {
                Location.CountryCode.Returns("LV");
                Location.CountryName.Returns("Latvia");

                Api.Location.Get().Returns(Observable.Return(Location));

                ApiFactory.CreateApiWith(Arg.Any<Credentials>()).Returns(Api);
            }
        }

        public sealed class TheConstructor : SignupViewModelTest
        {
            [Xunit.Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useApiFactory,
                bool useUserAccessManager,
                bool useAnalyticsService,
                bool useOnboardingStorage,
                bool userNavigationService,
                bool useApiErrorHandlingService,
                bool useLastTimeUsageStorage,
                bool useTimeService,
                bool useSchedulerProvider,
                bool useRxActionFactory)
            {
                var apiFactory = useApiFactory ? ApiFactory : null;
                var userAccessManager = useUserAccessManager ? UserAccessManager : null;
                var analyticsSerivce = useAnalyticsService ? AnalyticsService : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var navigationService = userNavigationService ? NavigationService : null;
                var apiErrorHandlingService = useApiErrorHandlingService ? ErrorHandlingService : null;
                var lastTimeUsageService = useLastTimeUsageStorage ? LastTimeUsageStorage : null;
                var timeService = useTimeService ? TimeService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SignupViewModel(
                        apiFactory,
                        userAccessManager,
                        analyticsSerivce,
                        onboardingStorage,
                        navigationService,
                        apiErrorHandlingService,
                        lastTimeUsageService,
                        timeService,
                        schedulerProvider,
                        rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheInitializeMethod : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task GetstheCurrentLocation()
            {
                await ViewModel.Initialize();

                await Api.Location.Received().Get();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheCountryButtonTitleToCountryNameWhenApiCallSucceeds()
            {
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.CountryButtonTitle.Subscribe(observer);

                await ViewModel.Initialize();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, Resources.SelectCountry),
                    ReactiveTest.OnNext(2, Location.CountryName)
                );
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheCountryButtonTitleToSelectCountryWhenApiCallFails()
            {
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.CountryButtonTitle.Subscribe(observer);

                Api.Location.Get().Returns(Observable.Throw<ILocation>(new Exception()));

                await ViewModel.Initialize();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, Resources.SelectCountry)
                );
            }

            [Fact, LogIfTooSlow]
            public async Task SetsFailedToGetCountryToTrueWhenApiCallFails()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsCountryErrorVisible.Subscribe(observer);

                Api.Location.Get().Returns(Observable.Throw<ILocation>(new Exception()));

                await ViewModel.Initialize();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, false),
                    ReactiveTest.OnNext(2, true)
                );
            }
        }

        public sealed class ThePickCountryMethod : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToSelectCountryViewModelPassingNullIfLocationApiFailed()
            {
                Api.Location.Get().Returns(Observable.Throw<ILocation>(new Exception()));
                await ViewModel.Initialize();

                ViewModel.PickCountry.Execute();

                TestScheduler.Start();
                await NavigationService.Received().Navigate<SelectCountryViewModel, long?, long?>(null);
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToSelectCountryViewModelPassingCountryIdIfLocationApiSucceeded()
            {
                await ViewModel.Initialize();
                var selectedCountryId = await new GetAllCountriesInteractor()
                    .Execute()
                    .Select(countries => countries.Single(country => country.CountryCode == Location.CountryCode))
                    .Select(country => country.Id);

                ViewModel.PickCountry.Execute();

                TestScheduler.Start();
                await NavigationService.Received().Navigate<SelectCountryViewModel, long?, long?>(selectedCountryId);
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheCountryButtonTitle()
            {
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.CountryButtonTitle.Subscribe(observer);

                var selectedCountry = await new GetAllCountriesInteractor()
                    .Execute()
                    .Select(countries => countries.Single(country => country.Id == 1));
                NavigationService
                    .Navigate<SelectCountryViewModel, long?, long?>(Arg.Any<long?>())
                    .Returns(selectedCountry.Id);
                await ViewModel.Initialize();

                ViewModel.PickCountry.Execute();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, Resources.SelectCountry),
                    ReactiveTest.OnNext(2, Location.CountryName),
                    ReactiveTest.OnNext(3, selectedCountry.Name)
                );
            }

            [Fact, LogIfTooSlow]
            public async Task SetsFailedToGetCountryToFalse()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsCountryErrorVisible.Subscribe(observer);

                Api.Location.Get().Returns(Observable.Throw<ILocation>(new Exception()));
                NavigationService
                    .Navigate<SelectCountryViewModel, long?, long?>(Arg.Any<long?>())
                    .Returns(1);
                await ViewModel.Initialize();

                ViewModel.PickCountry.Execute();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, false),
                    ReactiveTest.OnNext(2, true),
                    ReactiveTest.OnNext(3, false)
                );
            }
        }

        public sealed class TheGoogleSignupMethod : SignupViewModelTest
        {
            protected override void AdditionalViewModelSetup()
            {
                base.AdditionalViewModelSetup();
                ViewModel.Initialize().Wait();
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheTermsOfServiceViewModel()
            {
                ViewModel.GoogleSignup.Execute();

                TestScheduler.Start();
                await NavigationService.Received().Navigate<TermsOfServiceViewModel, bool>();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotTryToSignUpIfUserDoesNotAcceptTermsOfService()
            {
                NavigationService.Navigate<TermsOfServiceViewModel, bool>().Returns(false);

                ViewModel.GoogleSignup.Execute();

                TestScheduler.Start();
                UserAccessManager.DidNotReceive().SignUpWithGoogle(Arg.Any<bool>(), Arg.Any<int>());
            }

            [Fact, LogIfTooSlow]
            public void ShowsTheTermsOfServiceViewModelOnlyOnceIfUserAcceptsTheTerms()
            {
                NavigationService.Navigate<TermsOfServiceViewModel, bool>().Returns(true);
                UserAccessManager
                    .SignUpWithGoogle(Arg.Any<bool>(), Arg.Any<int>())
                    .Returns(Observable.Throw<ITogglDataSource>(new Exception()));

                ViewModel.GoogleSignup.Execute();
                ViewModel.GoogleSignup.Execute();

                TestScheduler.Start();
                NavigationService.Received(1).Navigate<TermsOfServiceViewModel, bool>();
            }

            public sealed class WhenUserAcceptsTheTermsOfService : SignupViewModelTest
            {
                protected override void AdditionalSetup()
                {
                    base.AdditionalSetup();

                    NavigationService.Navigate<TermsOfServiceViewModel, bool>().Returns(true);
                }

                protected override void AdditionalViewModelSetup()
                {
                    base.AdditionalViewModelSetup();
                    ViewModel.Initialize().Wait();
                }

                [Fact, LogIfTooSlow]
                public void CallsTheUserAccessManager()
                {
                    ViewModel.GoogleSignup.Execute();

                    TestScheduler.Start();
                    UserAccessManager.Received().SignUpWithGoogle(true, Arg.Any<int>());
                }

                [Fact, LogIfTooSlow]
                public void DoesNothingWhenThePageIsCurrentlyLoading()
                {
                    var never = Observable.Never<ITogglDataSource>();
                    UserAccessManager.SignUpWithGoogle(Arg.Any<bool>(), Arg.Any<int>()).Returns(never);
                    ViewModel.GoogleSignup.Execute();
                    ViewModel.GoogleSignup.Execute();

                    TestScheduler.Start();
                    UserAccessManager.Received(1).SignUpWithGoogle(Arg.Any<bool>(), Arg.Any<int>());
                }

                [Fact, LogIfTooSlow]
                public void SetsTheIsLoadingPropertyToTrue()
                {
                    var observer = TestScheduler.CreateObserver<bool>();
                    ViewModel.IsLoading.Subscribe(observer);

                    UserAccessManager.SignUpWithGoogle(true, Arg.Any<int>()).Returns(
                        Observable.Never<ITogglDataSource>());

                    ViewModel.GoogleSignup.Execute();

                    TestScheduler.Start();
                    observer.Messages.AssertEqual(
                        ReactiveTest.OnNext(1, false),
                        ReactiveTest.OnNext(2, true)
                    );
                }

                [Fact, LogIfTooSlow]
                public void TracksGoogleSignupEvent()
                {
                    UserAccessManager.SignUpWithGoogle(true, Arg.Any<int>()).Returns(
                        Observable.Return(Substitute.For<ITogglDataSource>()));

                    ViewModel.GoogleSignup.Execute();

                    TestScheduler.Start();
                    AnalyticsService.SignUp.Received().Track(AuthenticationMethod.Google);
                }

                [Fact, LogIfTooSlow]
                public void StopsTheViewModelLoadStateWhenItErrors()
                {
                    var observer = TestScheduler.CreateObserver<bool>();
                    ViewModel.IsLoading.Subscribe(observer);

                    UserAccessManager.SignUpWithGoogle(Arg.Any<bool>(), Arg.Any<int>()).Returns(
                        Observable.Throw<ITogglDataSource>(new GoogleLoginException(false)));

                    ViewModel.GoogleSignup.Execute();

                    TestScheduler.Start();
                    observer.Messages.AssertEqual(
                        ReactiveTest.OnNext(1, false),
                        ReactiveTest.OnNext(2, true),
                        ReactiveTest.OnNext(3, false)
                    );
                }

                [Fact, LogIfTooSlow]
                public void DoesNotNavigateWhenTheLoginFails()
                {
                    UserAccessManager.SignUpWithGoogle(Arg.Any<bool>(), Arg.Any<int>()).Returns(
                        Observable.Throw<ITogglDataSource>(new GoogleLoginException(false)));

                    ViewModel.GoogleSignup.Execute();

                    TestScheduler.Start();
                    NavigationService.DidNotReceive().Navigate<MainViewModel>();
                }

                [Fact, LogIfTooSlow]
                public void DoesNotDisplayAnErrorMessageWhenTheUserCancelsTheRequestOnTheGoogleService()
                {
                    var hasErrorObserver = TestScheduler.CreateObserver<bool>();
                    ViewModel.HasError.Subscribe(hasErrorObserver);
                    var errorTextObserver = TestScheduler.CreateObserver<string>();
                    ViewModel.ErrorMessage.Subscribe(errorTextObserver);

                    UserAccessManager.SignUpWithGoogle(Arg.Any<bool>(), Arg.Any<int>()).Returns(
                        Observable.Throw<ITogglDataSource>(new GoogleLoginException(true)));

                    ViewModel.GoogleSignup.Execute();

                    TestScheduler.Start();
                    errorTextObserver.Messages.AssertEqual(
                        ReactiveTest.OnNext(1, "")
                    );

                    hasErrorObserver.Messages.AssertEqual(
                        ReactiveTest.OnNext(2, false)
                    );
                }

                public sealed class WhenSignupSucceeds : SuccessfulSignupTest
                {
                    protected override void ExecuteCommand()
                    {
                        ViewModel.GoogleSignup.Execute();
                    }

                    protected override void AdditionalViewModelSetup()
                    {
                        base.AdditionalViewModelSetup();

                        ViewModel.Initialize().Wait();

                        UserAccessManager
                            .SignUpWithGoogle(true, Arg.Any<int>())
                            .Returns(Observable.Return(DataSource));

                        NavigationService.Navigate<TermsOfServiceViewModel, bool>().Returns(true);
                    }

                    [FsCheck.Xunit.Property]
                    public void SavesTheTimeOfLastLogin(DateTimeOffset now)
                    {
                        TimeService.CurrentDateTime.Returns(now);

                        var viewModel = CreateViewModel();

                        viewModel.Initialize().Wait();
                        viewModel.GoogleSignup.Execute();

                        TestScheduler.Start();
                        LastTimeUsageStorage.Received().SetLogin(now);
                    }
                }
            }
        }

        public sealed class TheTogglePasswordVisibilityMethod : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void SetsIsPasswordMaskedToFalseWhenItIsTrue()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsPasswordMasked.Subscribe(observer);

                ViewModel.TogglePasswordVisibility();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, true),
                    ReactiveTest.OnNext(2, false)
                );
            }

            [Fact, LogIfTooSlow]
            public void SetsIsPasswordMaskedToTrueWhenItIsFalse()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsPasswordMasked.Subscribe(observer);

                ViewModel.TogglePasswordVisibility();
                ViewModel.TogglePasswordVisibility();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, true),
                    ReactiveTest.OnNext(2, false),
                    ReactiveTest.OnNext(3, true)
                );
            }
        }

        public sealed class TheLoginMethod : SignupViewModelTest
        {
            [FsCheck.Xunit.Property]
            public void NavigatesToLoginViewModel(
                NonEmptyString email, NonEmptyString password)
            {
                ViewModel.SetEmail(Email.From(email.Get));
                ViewModel.SetPassword(Password.From(password.Get));

                ViewModel.Login.Execute();

                TestScheduler.Start();
                NavigationService
                    .Received()
                    .Navigate<LoginViewModel, CredentialsParameter>(
                        Arg.Is<CredentialsParameter>(parameter
                             => parameter.Email.Equals(Email.From(email.Get))
                             && parameter.Password.Equals(Password.From(password.Get))));
            }

            [Fact, LogIfTooSlow]
            public void PassesTheCredentialsToLoginViewModelIfUserTriedToSignUpWithExistingEmail()
            {
                var request = Substitute.For<IRequest>();
                request.Endpoint.Returns(new Uri("http://any.url.com"));
                var exception = new EmailIsAlreadyUsedException(
                    new BadRequestException(
                        request, Substitute.For<IResponse>()
                    )
                );
                UserAccessManager
                    .SignUp(Arg.Any<Email>(), Arg.Any<Password>(), true, Arg.Any<int>())
                    .Returns(
                        Observable.Throw<ITogglDataSource>(exception)
                    );
                NavigationService.Navigate<TermsOfServiceViewModel, bool>();
                ViewModel.SetEmail(ValidEmail);
                ViewModel.SetPassword(ValidPassword);
                ViewModel.Signup.Execute();
                TestScheduler.Start();
                TestScheduler.Stop();
                ViewModel.Login.Execute();

                TestScheduler.Start();
                NavigationService
                    .Received()
                    .Navigate<LoginViewModel, CredentialsParameter>(
                        Arg.Is<CredentialsParameter>(
                            parameter => parameter.Email.Equals(ValidEmail)
                            && parameter.Password.Equals(ValidPassword)
                        )
                    );
            }
        }

        public sealed class TheShakeFlags : SignupViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ReturnsNothingWhenEmailPasswordAndCountryAreValid()
            {
                var observer = TestScheduler.CreateObserver<ShakeTargets>();
                ViewModel.Shake.Subscribe(observer);

                await ViewModel.Initialize();

                ViewModel.SetEmail(ValidEmail);
                ViewModel.SetPassword(ValidPassword);

                ViewModel.Signup.Execute();

                TestScheduler.Start();
                observer.Messages.AssertEqual();
            }

            [Xunit.Theory]
            [InlineData("not an email", "123", true, ShakeTargets.Email | ShakeTargets.Password)]
            [InlineData("not an email", "1234567", true, ShakeTargets.Email)]
            [InlineData("this@is.email", "123", true, ShakeTargets.Password)]
            [InlineData("not an email", "123", false, ShakeTargets.Country | ShakeTargets.Email | ShakeTargets.Password)]
            [InlineData("not an email", "1234567", false, ShakeTargets.Country | ShakeTargets.Email)]
            [InlineData("this@is.email", "123", false, ShakeTargets.Country | ShakeTargets.Password)]
            public async Task ReturnsTheCorrectFlagForInvalidEmailPasswordOrCountry(string email, string password, bool countryIsSelected, ShakeTargets shakeTargets)
            {
                var observer = TestScheduler.CreateObserver<ShakeTargets>();
                ViewModel.Shake.Subscribe(observer);

                var currentEmail = Email.From(email);
                var currentPassword = Password.From(password);
                var expectedShakeTargets = shakeTargets;

                if (!countryIsSelected)
                {
                    Api.Location.Get().Returns(Observable.Throw<ILocation>(new Exception()));
                }

                await ViewModel.Initialize();

                ViewModel.SetEmail(currentEmail);
                ViewModel.SetPassword(currentPassword);

                ViewModel.Signup.Execute();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, expectedShakeTargets)
                );
            }
        }

        public sealed class TheSignupMethod : SignupViewModelTest
        {
            protected override void AdditionalViewModelSetup()
            {
                base.AdditionalViewModelSetup();

                ViewModel.SetEmail(ValidEmail);
                ViewModel.SetPassword(ValidPassword);

                ViewModel.Initialize().Wait();
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheTermsOfServiceViewModel()
            {
                ViewModel.Signup.Execute();

                TestScheduler.Start();
                await NavigationService.Received().Navigate<TermsOfServiceViewModel, bool>();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotTryToSignUpIfUserDoesNotAcceptTermsOfService()
            {
                NavigationService.Navigate<TermsOfServiceViewModel, bool>().Returns(false);

                ViewModel.Signup.Execute();

                TestScheduler.Start();
                UserAccessManager.DidNotReceive().SignUp(
                    Arg.Any<Email>(),
                    Arg.Any<Password>(),
                    Arg.Any<bool>(),
                    Arg.Any<int>());
            }

            [Fact, LogIfTooSlow]
            public void ShowsTheTermsOfServiceViewModelOnlyOnceIfUserAcceptsTheTerms()
            {
                NavigationService.Navigate<TermsOfServiceViewModel, bool>().Returns(true);
                UserAccessManager
                    .SignUp(Arg.Any<Email>(), Arg.Any<Password>(), Arg.Any<bool>(), Arg.Any<int>())
                    .Returns(Observable.Throw<ITogglDataSource>(new Exception()));

                ViewModel.Signup.Execute();
                ViewModel.Signup.Execute();

                TestScheduler.Start();
                NavigationService.Received(1).Navigate<TermsOfServiceViewModel, bool>();
            }

            public sealed class WhenUserAcceptsTheTermsOfService : SignupViewModelTest
            {
                protected override void AdditionalSetup()
                {
                    base.AdditionalSetup();

                    NavigationService.Navigate<TermsOfServiceViewModel, bool>().Returns(true);
                }

                protected override void AdditionalViewModelSetup()
                {
                    base.AdditionalViewModelSetup();

                    ViewModel.Initialize().Wait();

                    ViewModel.SetEmail(ValidEmail);
                    ViewModel.SetPassword(ValidPassword);
                    ViewModel.Signup.Execute();
                }

                [Fact, LogIfTooSlow]
                public void SetsIsLoadingToTrue()
                {
                    var observer = TestScheduler.CreateObserver<bool>();
                    ViewModel.IsLoading.Subscribe(observer);

                    TestScheduler.Start();
                    observer.Messages.AssertEqual(
                        ReactiveTest.OnNext(1, true)
                    );
                }

                [Fact, LogIfTooSlow]
                public void TriesToSignUp()
                {
                    UserAccessManager.Received().SignUp(
                        ValidEmail,
                        ValidPassword,
                        true,
                        Arg.Any<int>());
                }

                [Fact, LogIfTooSlow]
                public void TracksSignupEvent()
                {
                    AnalyticsService.SignUp.Received().Track(AuthenticationMethod.EmailAndPassword);
                }

                public sealed class WhenSignupSucceeds : SuccessfulSignupTest
                {
                    protected override void ExecuteCommand()
                    {
                        ViewModel.Signup.Execute();
                    }

                    protected override void AdditionalViewModelSetup()
                    {
                        base.AdditionalViewModelSetup();

                        ViewModel.Initialize().Wait();

                        ViewModel.SetEmail(ValidEmail);
                        ViewModel.SetPassword(ValidPassword);
                        UserAccessManager
                            .SignUp(Arg.Any<Email>(), Arg.Any<Password>(), Arg.Any<bool>(), Arg.Any<int>())
                            .Returns(Observable.Return(DataSource));
                        NavigationService.Navigate<TermsOfServiceViewModel, bool>().Returns(true);
                    }

                    [FsCheck.Xunit.Property]
                    public void SavesTheTimeOfLastLogin(DateTimeOffset now)
                    {
                        var viewModel = CreateViewModel();
                        viewModel.Initialize().Wait();
                        viewModel.SetEmail(ValidEmail);
                        viewModel.SetPassword(ValidPassword);
                        TimeService.CurrentDateTime.Returns(now);

                        viewModel.Signup.Execute();

                        TestScheduler.Start();
                        LastTimeUsageStorage.Received().SetLogin(now);
                    }
                }

                public sealed class WhenSignupFails : SignupViewModelTest
                {
                    private void prepareException(Exception exception)
                    {
                        UserAccessManager
                            .SignUp(Arg.Any<Email>(), Arg.Any<Password>(), Arg.Any<bool>(), Arg.Any<int>())
                            .Returns(Observable.Throw<ITogglDataSource>(exception));
                    }

                    protected override void AdditionalViewModelSetup()
                    {
                        base.AdditionalViewModelSetup();

                        ViewModel.Initialize().Wait();

                        ViewModel.SetEmail(ValidEmail);
                        ViewModel.SetPassword(ValidPassword);

                        NavigationService.Navigate<TermsOfServiceViewModel, bool>().Returns(true);
                    }

                    [Fact, LogIfTooSlow]
                    public void SetsIsLoadingToFalse()
                    {
                        var observer = TestScheduler.CreateObserver<bool>();
                        ViewModel.IsLoading.Subscribe(observer);

                        prepareException(new Exception());

                        ViewModel.Signup.Execute();

                        TestScheduler.Start();
                        observer.Messages.AssertEqual(
                            ReactiveTest.OnNext(1, false),
                            ReactiveTest.OnNext(2, true),
                            ReactiveTest.OnNext(3, false)
                        );
                    }

                    [Fact, LogIfTooSlow]
                    public void SetsIncorrectEmailOrPasswordErrorIfReceivedUnautghorizedException()
                    {
                        var observer = TestScheduler.CreateObserver<string>();
                        ViewModel.ErrorMessage.Subscribe(observer);

                        prepareException(new UnauthorizedException(
                            Substitute.For<IRequest>(),
                            Substitute.For<IResponse>()));

                        ViewModel.Signup.Execute();

                        TestScheduler.Start();
                        observer.Messages.AssertEqual(
                            ReactiveTest.OnNext(1, ""),
                            ReactiveTest.OnNext(2, Resources.IncorrectEmailOrPassword)
                        );
                    }

                    [Fact, LogIfTooSlow]
                    public void SetsEmailAlreadyUsedErrorIfReceivedEmailIsAlreadyusedException()
                    {
                        var observer = TestScheduler.CreateObserver<string>();
                        ViewModel.ErrorMessage.Subscribe(observer);

                        var request = Substitute.For<IRequest>();
                        request.Endpoint.Returns(new Uri("https://any.url.com"));
                        prepareException(new EmailIsAlreadyUsedException(
                            new BadRequestException(
                                request,
                                Substitute.For<IResponse>()
                            )
                        ));

                        ViewModel.Signup.Execute();

                        TestScheduler.Start();
                        observer.Messages.AssertEqual(
                            ReactiveTest.OnNext(1, ""),
                            ReactiveTest.OnNext(2, Resources.EmailIsAlreadyUsedError)
                        );
                    }

                    [Fact, LogIfTooSlow]
                    public void SetsGenereicErrorForAnyOtherException()
                    {
                        var observer = TestScheduler.CreateObserver<string>();
                        ViewModel.ErrorMessage.Subscribe(observer);

                        prepareException(new Exception());

                        ViewModel.Signup.Execute();

                        TestScheduler.Start();
                        observer.Messages.AssertEqual(
                            ReactiveTest.OnNext(1, ""),
                            ReactiveTest.OnNext(2, Resources.GenericSignUpError)
                        );
                    }

                    [Fact, LogIfTooSlow]
                    public void TracksTheEventAndException()
                    {
                        var exception = new Exception();
                        prepareException(exception);

                        ViewModel.Signup.Execute();

                        TestScheduler.Start();
                        AnalyticsService.UnknownSignUpFailure.Received()
                            .Track(exception.GetType().FullName, exception.Message);
                        AnalyticsService.Received().TrackAnonymized(exception);
                    }
                }
            }
        }

        public abstract class SuccessfulSignupTest : SignupViewModelTest
        {
            protected abstract void ExecuteCommand();

            [Fact, LogIfTooSlow]
            public void StartsSyncing()
            {
                ExecuteCommand();

                TestScheduler.Start();
                DataSource.SyncManager.Received().ForceFullSync();
            }

            [Fact, LogIfTooSlow]
            public void SetsIsNewUserToTrue()
            {
                ExecuteCommand();

                TestScheduler.Start();
                OnboardingStorage.Received().SetIsNewUser(true);
            }

            [Fact, LogIfTooSlow]
            public void SetsUserSignedUp()
            {
                ExecuteCommand();

                TestScheduler.Start();
                OnboardingStorage.Received().SetUserSignedUp();
            }

            [Fact, LogIfTooSlow]
            public void NavigatesToMainViewModel()
            {
                ExecuteCommand();

                TestScheduler.Start();
                NavigationService.Received().ForkNavigate<MainTabBarViewModel, MainViewModel>();
            }
        }

        public sealed class TheSignupEnabledProperty : SignupViewModelTest
        {
            protected override void AdditionalViewModelSetup()
            {
                base.AdditionalViewModelSetup();

                ViewModel.Initialize().Wait();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsTrueWhenEmailAndPasswordAreValidAndIsNotLoading()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.SignupEnabled.Subscribe(observer);

                ViewModel.SetEmail(ValidEmail);
                ViewModel.SetPassword(ValidPassword);

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(2, true)
                );
            }

            [Xunit.Theory]
            [InlineData("not an email", "123")]
            [InlineData("not an email", "1234567")]
            [InlineData("this@is.email", "123")]
            public void ReturnsFalseWhenEmailOrPasswordIsInvalid(string email, string password)
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.SignupEnabled.Subscribe(observer);

                ViewModel.SetEmail(Email.From(email));
                ViewModel.SetPassword(Password.From(password));

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(2, false)
                );
            }

            [Fact]
            public void ReturnsFlaseWhenIsLoading()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.SignupEnabled.Subscribe(observer);

                ViewModel.SetEmail(ValidEmail);
                ViewModel.SetPassword(ValidPassword);
                NavigationService.Navigate<TermsOfServiceViewModel, bool>().Returns(true);
                UserAccessManager
                    .SignUp(Arg.Any<Email>(), Arg.Any<Password>(), Arg.Any<bool>(), Arg.Any<int>())
                    .Returns(Observable.Never<ITogglDataSource>());
                ViewModel.Signup.Execute();

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(2, true),
                    ReactiveTest.OnNext(3, false)
                );
            }
        }

        public sealed class ThePrepareMethod : SignupViewModelTest
        {
            [FsCheck.Xunit.Property]
            public void SetsTheEmail(NonEmptyString emailString)
            {
                var viewModel = CreateViewModel();
                var email = Email.From(emailString.Get);
                var password = Password.Empty;
                var parameter = CredentialsParameter.With(email, password);
                var expectedValues = new[] { Email.Empty.ToString(), email.TrimmedEnd().ToString() }.Distinct();
                var actualValues = new List<string>();
                viewModel.Email.Subscribe(actualValues.Add);

                viewModel.Prepare(parameter);

                TestScheduler.Start();
                CollectionAssert.AreEqual(expectedValues, actualValues);
            }

            [FsCheck.Xunit.Property]
            public void SetsThePassword(NonEmptyString passwordString)
            {
                var viewModel = CreateViewModel();
                var email = Email.Empty;
                var password = Password.From(passwordString.Get);
                var parameter = CredentialsParameter.With(email, password);
                var expectedValues = new[] { Password.Empty.ToString(), password.ToString() };
                var actualValues = new List<string>();
                viewModel.Password.Subscribe(actualValues.Add);

                viewModel.Prepare(parameter);

                TestScheduler.Start();
                CollectionAssert.AreEqual(expectedValues, actualValues);
            }
        }
    }
}
