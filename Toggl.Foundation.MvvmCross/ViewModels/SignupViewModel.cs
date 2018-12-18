using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Interactors.Location;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SignupViewModel : MvxViewModel<CredentialsParameter>
    {
        [Flags]
        public enum ShakeTargets
        {
            None = 0,
            Email = 1 << 0,
            Password = 1 << 1,
            Country = 1 << 2
        }

        private readonly IApiFactory apiFactory;
        private readonly IUserAccessManager userAccessManager;
        private readonly IAnalyticsService analyticsService;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IForkingNavigationService navigationService;
        private readonly IErrorHandlingService errorHandlingService;
        private readonly ILastTimeUsageStorage lastTimeUsageStorage;
        private readonly ITimeService timeService;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IRxActionFactory rxActionFactory;

        private IDisposable getCountrySubscription;
        private IDisposable signupDisposable;
        private bool termsOfServiceAccepted;
        private List<ICountry> allCountries;
        private long? countryId;

        private readonly Subject<ShakeTargets> shakeSubject = new Subject<ShakeTargets>();
        private readonly Subject<bool> isShowPasswordButtonVisibleSubject = new Subject<bool>();
        private readonly BehaviorSubject<bool> isLoadingSubject = new BehaviorSubject<bool>(false);
        private readonly BehaviorSubject<string> errorMessageSubject = new BehaviorSubject<string>(string.Empty);
        private readonly BehaviorSubject<bool> isPasswordMaskedSubject = new BehaviorSubject<bool>(true);
        private readonly BehaviorSubject<Email> emailSubject = new BehaviorSubject<Email>(Multivac.Email.Empty);
        private readonly BehaviorSubject<Password> passwordSubject = new BehaviorSubject<Password>(Multivac.Password.Empty);
        private readonly BehaviorSubject<string> countryNameSubject = new BehaviorSubject<string>(Resources.SelectCountry);
        private readonly BehaviorSubject<bool> isCountryErrorVisibleSubject = new BehaviorSubject<bool>(false);

        public IObservable<string> CountryButtonTitle { get; }
        public IObservable<bool> IsCountryErrorVisible { get; }
        public IObservable<string> Email { get; }
        public IObservable<string> Password { get; }
        public IObservable<bool> HasError { get; }
        public IObservable<bool> IsLoading { get; }
        public IObservable<bool> SignupEnabled { get; }
        public IObservable<ShakeTargets> Shake { get; }
        public IObservable<string> ErrorMessage { get; }
        public IObservable<bool> IsPasswordMasked { get; }
        public IObservable<bool> IsShowPasswordButtonVisible { get; }

        public UIAction Login { get; }
        public UIAction Signup { get; }
        public UIAction GoogleSignup { get; }
        public UIAction PickCountry { get; }

        public SignupViewModel(
            IApiFactory apiFactory,
            IUserAccessManager userAccessManager,
            IAnalyticsService analyticsService,
            IOnboardingStorage onboardingStorage,
            IForkingNavigationService navigationService,
            IErrorHandlingService errorHandlingService,
            ILastTimeUsageStorage lastTimeUsageStorage,
            ITimeService timeService,
            ISchedulerProvider schedulerProvider,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(apiFactory, nameof(apiFactory));
            Ensure.Argument.IsNotNull(userAccessManager, nameof(userAccessManager));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(errorHandlingService, nameof(errorHandlingService));
            Ensure.Argument.IsNotNull(lastTimeUsageStorage, nameof(lastTimeUsageStorage));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.apiFactory = apiFactory;
            this.userAccessManager = userAccessManager;
            this.analyticsService = analyticsService;
            this.onboardingStorage = onboardingStorage;
            this.navigationService = navigationService;
            this.errorHandlingService = errorHandlingService;
            this.lastTimeUsageStorage = lastTimeUsageStorage;
            this.timeService = timeService;
            this.schedulerProvider = schedulerProvider;
            this.rxActionFactory = rxActionFactory;

            Login = rxActionFactory.FromAsync(login);
            Signup = rxActionFactory.FromAsync(signup);
            GoogleSignup = rxActionFactory.FromAsync(googleSignup);
            PickCountry = rxActionFactory.FromAsync(pickCountry);

            var emailObservable = emailSubject.Select(email => email.TrimmedEnd());

            Shake = shakeSubject.AsDriver(this.schedulerProvider);

            Email = emailObservable
                .Select(email => email.ToString())
                .DistinctUntilChanged()
                .AsDriver(this.schedulerProvider);

            Password = passwordSubject
                .Select(password => password.ToString())
                .DistinctUntilChanged()
                .AsDriver(this.schedulerProvider);

            IsLoading = isLoadingSubject
                .DistinctUntilChanged()
                .AsDriver(this.schedulerProvider);

            IsCountryErrorVisible = isCountryErrorVisibleSubject
                .DistinctUntilChanged()
                .AsDriver(this.schedulerProvider);

            ErrorMessage = errorMessageSubject
                .DistinctUntilChanged()
                .AsDriver(this.schedulerProvider);

            CountryButtonTitle = countryNameSubject
                .DistinctUntilChanged()
                .AsDriver(this.schedulerProvider);

            IsPasswordMasked = isPasswordMaskedSubject
                .DistinctUntilChanged()
                .AsDriver(this.schedulerProvider);

            IsShowPasswordButtonVisible = Password
                .Select(password => password.Length > 1)
                .CombineLatest(isShowPasswordButtonVisibleSubject.AsObservable(), CommonFunctions.And)
                .DistinctUntilChanged()
                .AsDriver(this.schedulerProvider);

            HasError = ErrorMessage
                .Select(string.IsNullOrEmpty)
                .Select(CommonFunctions.Invert)
                .AsDriver(this.schedulerProvider);

            SignupEnabled = emailObservable
                .CombineLatest(
                    passwordSubject.AsObservable(),
                    IsLoading,
                    countryNameSubject.AsObservable(),
                    (email, password, isLoading, countryName) => email.IsValid && password.IsValid && !isLoading && (countryName != Resources.SelectCountry))
                .DistinctUntilChanged()
                .AsDriver(this.schedulerProvider);
        }

        public override void Prepare(CredentialsParameter parameter)
        {
            emailSubject.OnNext(parameter.Email);
            passwordSubject.OnNext(parameter.Password);
        }

        public void SetEmail(Email email)
            => emailSubject.OnNext(email);

        public void SetPassword(Password password)
            => passwordSubject.OnNext(password);

        public void SetIsShowPasswordButtonVisible(bool visible)
            => isShowPasswordButtonVisibleSubject.OnNext(visible);

        public override async Task Initialize()
        {
            await base.Initialize();

            allCountries = await new GetAllCountriesInteractor().Execute();

            var api = apiFactory.CreateApiWith(Credentials.None);
            getCountrySubscription = new GetCurrentLocationInteractor(api)
                .Execute()
                .Select(location => allCountries.Single(country => country.CountryCode == location.CountryCode))
                .Subscribe(
                    setCountryIfNeeded,
                    _ => setCountryErrorIfNeeded(),
                    () =>
                    {
                        getCountrySubscription?.Dispose();
                        getCountrySubscription = null;
                    }
                );
        }

        private void setCountryIfNeeded(ICountry country)
        {
            if (countryId.HasValue) return;
            countryId = country.Id;
            countryNameSubject.OnNext(country.Name);
        }

        private void setCountryErrorIfNeeded()
        {
            if (countryId.HasValue) return;

            isCountryErrorVisibleSubject.OnNext(true);
        }

        private async Task signup()
        {
            var shakeTargets = ShakeTargets.None;
            if (!emailSubject.Value.IsValid)
            {
                shakeTargets |= ShakeTargets.Email;
            }
            if (!passwordSubject.Value.IsValid)
            {
                shakeTargets |= ShakeTargets.Password;
            }
            if (!countryId.HasValue)
            {
                shakeTargets |= ShakeTargets.Country;
            }

            if (shakeTargets != ShakeTargets.None)
            {
                shakeSubject.OnNext(shakeTargets);
                return;
            }

            await requestAcceptanceOfTermsAndConditionsIfNeeded();

            if (!termsOfServiceAccepted || isLoadingSubject.Value) return;

            isLoadingSubject.OnNext(true);
            errorMessageSubject.OnNext(string.Empty);

            signupDisposable =
                userAccessManager
                    .SignUp(emailSubject.Value, passwordSubject.Value, termsOfServiceAccepted, (int)countryId.Value)
                    .Track(analyticsService.SignUp, AuthenticationMethod.EmailAndPassword)
                    .Subscribe(onDataSource, onError, onCompleted);
        }

        private async void onDataSource(ITogglDataSource dataSource)
        {
            lastTimeUsageStorage.SetLogin(timeService.CurrentDateTime);

            await dataSource.SyncManager.ForceFullSync();

            onboardingStorage.SetIsNewUser(true);
            onboardingStorage.SetUserSignedUp();

            await navigationService.ForkNavigate<MainTabBarViewModel, MainViewModel>();
        }

        private void onError(Exception exception)
        {
            isLoadingSubject.OnNext(false);
            onCompleted();

            if (errorHandlingService.TryHandleDeprecationError(exception))
                return;

            switch (exception)
            {
                case UnauthorizedException forbidden:
                    errorMessageSubject.OnNext(Resources.IncorrectEmailOrPassword);
                    break;
                case GoogleLoginException googleEx when googleEx.LoginWasCanceled:
                    errorMessageSubject.OnNext(string.Empty);
                    break;
                case EmailIsAlreadyUsedException _:
                    errorMessageSubject.OnNext(Resources.EmailIsAlreadyUsedError);
                    break;
                default:
                    analyticsService.UnknownSignUpFailure.Track(exception.GetType().FullName, exception.Message);
                    analyticsService.TrackAnonymized(exception);
                    errorMessageSubject.OnNext(Resources.GenericSignUpError);
                    break;
            }
        }

        private void onCompleted()
        {
            signupDisposable?.Dispose();
            signupDisposable = null;
        }

        private async Task googleSignup()
        {
            if (!countryId.HasValue)
            {
                shakeSubject.OnNext(ShakeTargets.Country);
                return;
            }

            await requestAcceptanceOfTermsAndConditionsIfNeeded();

            if (!termsOfServiceAccepted || isLoadingSubject.Value) return;

            isLoadingSubject.OnNext(true);
            errorMessageSubject.OnNext(string.Empty);

            signupDisposable = userAccessManager
                .SignUpWithGoogle(termsOfServiceAccepted, (int)countryId.Value)
                .Track(analyticsService.SignUp, AuthenticationMethod.Google)
                .Subscribe(onDataSource, onError, onCompleted);
        }

        public void TogglePasswordVisibility()
            => isPasswordMaskedSubject.OnNext(!isPasswordMaskedSubject.Value);

        private async Task pickCountry()
        {
            getCountrySubscription?.Dispose();
            getCountrySubscription = null;

            var selectedCountryId = await navigationService
                .Navigate<SelectCountryViewModel, long?, long?>(countryId);

            if (selectedCountryId == null)
            {
                setCountryErrorIfNeeded();
                return;
            }

            var selectedCountry = allCountries
                .Single(country => country.Id == selectedCountryId.Value);

            isCountryErrorVisibleSubject.OnNext(false);
            countryId = selectedCountry.Id;
            countryNameSubject.OnNext(selectedCountry.Name);
        }

        private Task login()
        {
            if (isLoadingSubject.Value)
                return Task.CompletedTask;

            var parameter = CredentialsParameter.With(emailSubject.Value, passwordSubject.Value);
            return navigationService.Navigate<LoginViewModel, CredentialsParameter>(parameter);
        }

        private async Task<bool> requestAcceptanceOfTermsAndConditionsIfNeeded()
        {
            if (termsOfServiceAccepted)
                return true;

            termsOfServiceAccepted = await navigationService.Navigate<TermsOfServiceViewModel, bool>();
            return termsOfServiceAccepted;
        }
    }
}
