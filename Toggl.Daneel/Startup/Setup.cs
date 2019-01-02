using System;
using System.Reactive.Concurrency;
using Foundation;
using MvvmCross;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Ios.Core;
using MvvmCross.Platforms.Ios.Presenters;
using MvvmCross.Plugin;
using MvvmCross.ViewModels;
using Toggl.Daneel.Presentation;
using Toggl.Daneel.Services;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Services;
using Toggl.Foundation.Suggestions;
using Toggl.PrimeRadiant.Realm;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Network;
using ColorPlugin = MvvmCross.Plugin.Color.Platforms.Ios.Plugin;
using VisibilityPlugin = MvvmCross.Plugin.Visibility.Platforms.Ios.Plugin;

namespace Toggl.Daneel
{
    public partial class Setup : MvxIosSetup<App<OnboardingViewModel>>
    {
        private const int maxNumberOfSuggestions = 3;

        private IAnalyticsService analyticsService;
        private IForkingNavigationService navigationService;

#if USE_PRODUCTION_API
        private const ApiEnvironment environment = ApiEnvironment.Production;
#else
        private const ApiEnvironment environment = ApiEnvironment.Staging;
#endif

        protected override IMvxIosViewPresenter CreateViewPresenter()
            => new TogglPresenter(ApplicationDelegate, Window);

        protected override IMvxNavigationService InitializeNavigationService(IMvxViewModelLocatorCollection collection)
        {
            analyticsService = new AnalyticsServiceIos();

            var loader = CreateViewModelLoader(collection);
            Mvx.RegisterSingleton<IMvxViewModelLoader>(loader);

            navigationService = new NavigationService(null, loader, analyticsService, Platform.Daneel);

            Mvx.RegisterSingleton<IForkingNavigationService>(navigationService);
            Mvx.RegisterSingleton<IMvxNavigationService>(navigationService);
            return navigationService;
        }

        protected override void InitializeApp(IMvxPluginManager pluginManager, IMvxApplication app)
        {
#if !USE_PRODUCTION_API
            System.Net.ServicePointManager.ServerCertificateValidationCallback
                  += (sender, certificate, chain, sslPolicyErrors) => true;
#endif

            const string clientName = "Daneel";
            const string remoteConfigDefaultsFileName = "RemoteConfigDefaults";
            var version = NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"].ToString();
            var database = new Database();
            var scheduler = Scheduler.Default;
            var timeService = new TimeService(scheduler);
            var topViewControllerProvider = (ITopViewControllerProvider)Presenter;
            var mailService = new MailServiceIos(topViewControllerProvider);
            var dialogService = new DialogServiceIos(topViewControllerProvider);
            var platformInfo = new PlatformInfoIos();
            var suggestionProviderContainer = new SuggestionProviderContainer(
                new MostUsedTimeEntrySuggestionProvider(database, timeService, maxNumberOfSuggestions)
            );
            var intentDonationService = new IntentDonationServiceIos();
            var privateSharedStorageService = new PrivateSharedStorageServiceIos();

            var appVersion = Version.Parse(version);
            var keyValueStorage = new UserDefaultsStorageIos();
            var permissionsService = new PermissionsServiceIos();
            var userAgent = new UserAgent(clientName, version);
            var settingsStorage = new SettingsStorage(Version.Parse(version), keyValueStorage);
            var remoteConfigService = new RemoteConfigServiceIos();
            remoteConfigService.SetupDefaults(remoteConfigDefaultsFileName);
            var schedulerProvider = new IOSSchedulerProvider();
            var calendarService = new CalendarServiceIos(permissionsService);
            var notificationService = new NotificationServiceIos(permissionsService, timeService);
            var backgroundSyncService = new BackgroundSyncServiceIos();
            var backgroundService = new BackgroundService(timeService);
            var automaticSyncingService = new AutomaticSyncingService(backgroundService, timeService, analyticsService);

            var foundation =
                TogglFoundation
                    .ForClient(userAgent, appVersion)
                    .WithDatabase(database)
                    .WithScheduler(scheduler)
                    .WithMailService(mailService)
                    .WithTimeService(timeService)
                    .WithApiEnvironment(environment)
                    .WithGoogleService<GoogleServiceIos>()
                    .WithRatingService<RatingServiceIos>()
                    .WithLicenseProvider<LicenseProviderIos>()
                    .WithAnalyticsService(analyticsService)
                    .WithSchedulerProvider(schedulerProvider)
                    .WithRemoteConfigService(remoteConfigService)
                    .WithNotificationService(notificationService)
                    .WithApiFactory(new ApiFactory(environment, userAgent))
                    .WithBackgroundService(backgroundService)
                    .WithAutomaticSyncingService(automaticSyncingService)
                    .WithApplicationShortcutCreator(new ApplicationShortcutCreator())
                    .WithSuggestionProviderContainer(suggestionProviderContainer)
                    .WithIntentDonationService(intentDonationService)
                    .WithStopwatchProvider<FirebaseStopwatchProviderIos>()
                    .WithPrivateSharedStorageService(privateSharedStorageService)
                    .WithPlatformInfo(platformInfo)
                    .WithBackgroundSyncService(backgroundSyncService)

                    .StartRegisteringPlatformServices()
                    .WithDialogService(dialogService)
                    .WithLastTimeUsageStorage(settingsStorage)
                    .WithBrowserService<BrowserServiceIos>()
                    .WithKeyValueStorage(keyValueStorage)
                    .WithUserPreferences(settingsStorage)
                    .WithCalendarService(calendarService)
                    .WithOnboardingStorage(settingsStorage)
                    .WithNavigationService(navigationService)
                    .WithPermissionsService(permissionsService)
                    .WithAccessRestrictionStorage(settingsStorage)
                    .WithPasswordManagerService<OnePasswordServiceIos>()
                    .WithErrorHandlingService(new ErrorHandlingService(navigationService, settingsStorage))
                    .WithFeedbackService(new FeedbackService(userAgent, mailService, dialogService, platformInfo))
                    .WithRxActionFactory(new RxActionFactory(schedulerProvider))
                    .Build();

            foundation.RevokeNewUserIfNeeded().Initialize();

            base.InitializeApp(pluginManager, app);
        }

        // Skip the sluggish and reflection-based manager and load our plugins by hand
        protected override IMvxPluginManager InitializePluginFramework()
        {
            LoadPlugins(null);
            return null;
        }

        public override void LoadPlugins(IMvxPluginManager pluginManager)
        {
            new ColorPlugin().Load();
            new VisibilityPlugin().Load();
        }

        protected override void PerformBootstrapActions()
        {
            // This method uses reflection to find classes that inherit from
            // IMvxBootstrapAction, creates instances of these classes and then
            // calls their Run method. We can skip it since we don't have such classes.
        }
    }
}
