using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.ViewModels;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class LoginViewModel : MvxViewModel<CredentialsParameter>
    {
        [Flags]
        public enum ShakeTargets
        {
            None = 0,
            Email = 1,
            Password = 2
        }

        private readonly IUserAccessManager userAccessManager;
        private readonly IAnalyticsService analyticsService;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IForkingNavigationService navigationService;
        private readonly IPasswordManagerService passwordManagerService;
        private readonly IErrorHandlingService errorHandlingService;
        private readonly ILastTimeUsageStorage lastTimeUsageStorage;
        private readonly ITimeService timeService;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IRxActionFactory rxActionFactory;

        private IDisposable loginDisposable;

        private readonly Subject<ShakeTargets> shakeSubject = new Subject<ShakeTargets>();
        private readonly Subject<bool> isShowPasswordButtonVisibleSubject = new Subject<bool>();
        private readonly BehaviorSubject<bool> isLoadingSubject = new BehaviorSubject<bool>(false);
        private readonly BehaviorSubject<string> errorMessageSubject = new BehaviorSubject<string>("");
        private readonly BehaviorSubject<bool> isPasswordMaskedSubject = new BehaviorSubject<bool>(true);
        private readonly BehaviorSubject<Email> emailSubject = new BehaviorSubject<Email>(Multivac.Email.Empty);
        private readonly BehaviorSubject<Password> passwordSubject = new BehaviorSubject<Password>(Multivac.Password.Empty);

        public bool IsPasswordManagerAvailable { get; }
        public IObservable<string> Email { get; }
        public IObservable<string> Password { get; }
        public IObservable<bool> HasError { get; }
        public IObservable<bool> IsLoading { get; }
        public IObservable<bool> LoginEnabled { get; }
        public IObservable<ShakeTargets> Shake { get; }
        public IObservable<string> ErrorMessage { get; }
        public IObservable<bool> IsPasswordMasked { get; }
        public IObservable<bool> IsShowPasswordButtonVisible { get; }

        public UIAction Signup { get; }
        public UIAction ForgotPassword { get; }
        public UIAction StartPasswordManager { get; }

        public LoginViewModel(
            IUserAccessManager userAccessManager,
            IAnalyticsService analyticsService,
            IOnboardingStorage onboardingStorage,
            IForkingNavigationService navigationService,
            IPasswordManagerService passwordManagerService,
            IErrorHandlingService errorHandlingService,
            ILastTimeUsageStorage lastTimeUsageStorage,
            ITimeService timeService,
            ISchedulerProvider schedulerProvider,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(userAccessManager, nameof(userAccessManager));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(passwordManagerService, nameof(passwordManagerService));
            Ensure.Argument.IsNotNull(errorHandlingService, nameof(errorHandlingService));
            Ensure.Argument.IsNotNull(lastTimeUsageStorage, nameof(lastTimeUsageStorage));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.timeService = timeService;
            this.userAccessManager = userAccessManager;
            this.analyticsService = analyticsService;
            this.onboardingStorage = onboardingStorage;
            this.navigationService = navigationService;
            this.errorHandlingService = errorHandlingService;
            this.lastTimeUsageStorage = lastTimeUsageStorage;
            this.passwordManagerService = passwordManagerService;
            this.schedulerProvider = schedulerProvider;

            var emailObservable = emailSubject.Select(email => email.TrimmedEnd());

            Signup = rxActionFactory.FromAsync(signup);
            ForgotPassword = rxActionFactory.FromAsync(forgotPassword);
            StartPasswordManager = rxActionFactory.FromAsync(startPasswordManager);

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

            ErrorMessage = errorMessageSubject
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

            LoginEnabled = emailObservable
                .CombineLatest(
                    passwordSubject.AsObservable(),
                    IsLoading,
                    (email, password, isLoading) => email.IsValid && password.IsValid && !isLoading)
                .DistinctUntilChanged()
                .AsDriver(this.schedulerProvider);

            IsPasswordManagerAvailable = passwordManagerService.IsAvailable;
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

        public void Login()
        {
            var shakeTargets = ShakeTargets.None;
            shakeTargets |= emailSubject.Value.IsValid ? ShakeTargets.None : ShakeTargets.Email;
            shakeTargets |= passwordSubject.Value.IsValid ? ShakeTargets.None : ShakeTargets.Password;

            if (shakeTargets != ShakeTargets.None)
            {
                shakeSubject.OnNext(shakeTargets);
                return;
            }

            if (isLoadingSubject.Value) return;

            isLoadingSubject.OnNext(true);
            errorMessageSubject.OnNext("");

            loginDisposable =
                userAccessManager
                    .Login(emailSubject.Value, passwordSubject.Value)
                    .Track(analyticsService.Login, AuthenticationMethod.EmailAndPassword)
                    .Subscribe(onDataSource, onError, onCompleted);
        }

        public void TogglePasswordVisibility()
            => isPasswordMaskedSubject.OnNext(!isPasswordMaskedSubject.Value);

        public void GoogleLogin()
        {
            if (isLoadingSubject.Value) return;

            isLoadingSubject.OnNext(true);

            loginDisposable = userAccessManager
                .LoginWithGoogle()
                .Track(analyticsService.Login, AuthenticationMethod.Google)
                .Subscribe(onDataSource, onError, onCompleted);
        }

        private Task signup()
        {
            if (isLoadingSubject.Value)
                return Task.CompletedTask;

            var parameter = CredentialsParameter.With(emailSubject.Value, passwordSubject.Value);
            return navigationService.Navigate<SignupViewModel, CredentialsParameter>(parameter);
        }


        private async Task forgotPassword()
        {
            if (isLoadingSubject.Value) return;

            var emailParameter = EmailParameter.With(emailSubject.Value);
            emailParameter = await navigationService
                .Navigate<ForgotPasswordViewModel, EmailParameter, EmailParameter>(emailParameter);
            if (emailParameter != null)
                emailSubject.OnNext(emailParameter.Email);
        }

        private async Task startPasswordManager()
        {
            if (!passwordManagerService.IsAvailable) return;
            if (isLoadingSubject.Value) return;

            analyticsService.PasswordManagerButtonClicked.Track();

            var loginInfo = await passwordManagerService.GetLoginInformation();

            emailSubject.OnNext(loginInfo.Email);
            if (!emailSubject.Value.IsValid) return;
            analyticsService.PasswordManagerContainsValidEmail.Track();

            passwordSubject.OnNext(loginInfo.Password);
            if (!passwordSubject.Value.IsValid) return;
            analyticsService.PasswordManagerContainsValidPassword.Track();

            Login();
        }

        private async void onDataSource(ITogglDataSource dataSource)
        {
            lastTimeUsageStorage.SetLogin(timeService.CurrentDateTime);

            await dataSource.SyncManager.ForceFullSync();

            onboardingStorage.SetIsNewUser(false);

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
                    errorMessageSubject.OnNext("");
                    break;
                default:
                    analyticsService.UnknownLoginFailure.Track(exception.GetType().FullName, exception.Message);
                    analyticsService.TrackAnonymized(exception);
                    errorMessageSubject.OnNext(Resources.GenericLoginError);
                    break;
            }
        }

        private void onCompleted()
        {
            loginDisposable?.Dispose();
            loginDisposable = null;
        }
    }
}
