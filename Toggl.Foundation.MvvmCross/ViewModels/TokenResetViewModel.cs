using MvvmCross.ViewModels;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class TokenResetViewModel : MvxViewModel
    {
        private readonly IUserAccessManager userAccessManager;
        private readonly ITogglDataSource dataSource;
        private readonly IDialogService dialogService;
        private readonly IForkingNavigationService navigationService;
        private readonly IUserPreferences userPreferences;
        private readonly IAnalyticsService analyticsService;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IRxActionFactory rxActionFactory;

        private readonly BehaviorSubject<string> errorSubject = new BehaviorSubject<string>(string.Empty);
        private readonly BehaviorSubject<Email> emailSubject = new BehaviorSubject<Email>(Multivac.Email.Empty);
        private readonly BehaviorSubject<Password> passwordSubject = new BehaviorSubject<Password>(Multivac.Password.Empty);
        private readonly BehaviorSubject<bool> isPasswordMaskedSubject = new BehaviorSubject<bool>(true);

        private bool needsSync;

        public IObservable<Email> Email { get; }
        public IObservable<Password> Password { get; }
        public IObservable<bool> IsPasswordMasked { get; }

        public IObservable<bool> HasError { get; }
        public IObservable<string> Error { get; }

        public IObservable<bool> IsLoading { get; }

        public IObservable<bool> NextIsEnabled { get; }

        public UIAction Done { get; private set; }
        public UIAction SignOut { get; private set; }
        public UIAction TogglePasswordVisibility { get; private set; }
        public InputAction<string> SetPassword { get; private set; }

        public TokenResetViewModel(
            IUserAccessManager userAccessManager,
            ITogglDataSource dataSource,
            IDialogService dialogService,
            IForkingNavigationService navigationService,
            IUserPreferences userPreferences,
            IAnalyticsService analyticsService,
            ISchedulerProvider schedulerProvider,
            IRxActionFactory rxActionFactory
        )
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(userAccessManager, nameof(userAccessManager));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.dataSource = dataSource;
            this.userAccessManager = userAccessManager;
            this.dialogService = dialogService;
            this.navigationService = navigationService;
            this.userPreferences = userPreferences;
            this.analyticsService = analyticsService;
            this.schedulerProvider = schedulerProvider;
            this.rxActionFactory = rxActionFactory;

            Error = errorSubject
                .AsDriver(schedulerProvider);

            Email = emailSubject
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            IsPasswordMasked = isPasswordMaskedSubject
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            Password = passwordSubject
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            HasError = Error
                .Select(error => !string.IsNullOrEmpty(error))
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            TogglePasswordVisibility = rxActionFactory.FromAction(togglePasswordVisibility);
            SetPassword = rxActionFactory.FromAction<string>(setPassword);

            Done = rxActionFactory.FromObservable(done);
            SignOut = rxActionFactory.FromAsync(signout);

            IsLoading = Done.Executing
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            NextIsEnabled = Password
                .CombineLatest(Done.Executing, (password, isExecuting) => password.IsValid && !isExecuting)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            needsSync = await dataSource.HasUnsyncedData();
            var user = await dataSource.User.Current.FirstAsync();

            emailSubject.OnNext(user.Email);
        }

        private void setPassword(string password)
        {
            passwordSubject.OnNext(Multivac.Password.From(password));
        }

        private void togglePasswordVisibility()
        {
            isPasswordMaskedSubject.OnNext(!isPasswordMaskedSubject.Value);
        }

        private void output(string text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
        }

        private async Task signout()
        {
            if (needsSync)
            {
                var userConfirmedLoggingOut = await askToLogOut();
                if (!userConfirmedLoggingOut)
                    return;
            }

            analyticsService.Logout.Track(LogoutSource.TokenReset);
            userPreferences.Reset();

            await userAccessManager.Logout();
            await navigationService.Navigate<LoginViewModel>();
        }

        private IObservable<Unit> done() =>
            Observable.Create<Unit>(observer =>
            {
                if (!passwordSubject.Value.IsValid)
                {
                    observer.OnError(new InvalidOperationException());
                    return Disposable.Empty;
                }

                userAccessManager
                    .RefreshToken(passwordSubject.Value)
                    .Subscribe(onDataSource, error =>
                    {
                        onError(error);
                        observer.OnError(error);
                    }, observer.CompleteWithUnit);

                return Disposable.Empty;
            });

        private void onDataSource(ITogglDataSource newDataSource)
        {
            newDataSource.SyncManager.ForceFullSync().Subscribe();

            navigationService.ForkNavigate<MainTabBarViewModel, MainViewModel>();
        }

        private void onError(Exception ex)
        {
            var error = ex is ForbiddenException
                ? Resources.IncorrectPassword
                : Resources.GenericLoginError;

            errorSubject.OnNext(error);
        }

        private IObservable<bool> askToLogOut()
            => dialogService.Confirm(
                Resources.AreYouSure,
                Resources.SettingsUnsyncedMessage,
                Resources.SettingsDialogButtonSignOut,
                Resources.Cancel);
    }
}
