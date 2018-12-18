using System;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Login;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Services;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant;
using Xunit;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Analytics;
using System.Reactive.Concurrency;
using Toggl.Multivac;
using Toggl.Ultrawave.Network;
using IStopwatchProvider = Toggl.Foundation.Diagnostics.IStopwatchProvider;

namespace Toggl.Foundation.Tests
{
    public sealed class FoundationTests
    {
        public class TheCreateMethod
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool userAgent,
                bool useVersion,
                bool useDatabase,
                bool useScheduler,
                bool useApiFactory,
                bool useTimeService,
                bool useMailService,
                bool usePlatformInfo,
                bool useRatingService,
                bool useGoogleService,
                bool useLicenseProvider,
                bool useAnalyticsService,
                bool useStopwatchProvider,
                bool useBackgroundService,
                bool useBackgroundSyncService,
                bool useSchedulerProvider,
                bool usePlatformConstants,
                bool useNotificationService,
                bool useRemoteConfigService,
                bool useIntentDonationService,
                bool useApplicationShortcutCreator,
                bool usePrivateSharedStorageService,
                bool useSuggestionProviderContainer)
            {
                var version = useVersion ? Version.Parse("1.0") : null;
                var platformInfo = usePlatformInfo ? new PlatformInfo() : null;
                var agent = userAgent ? new UserAgent("Some Client", "1.0") : null;
                var scheduler = useScheduler ? Substitute.For<IScheduler>() : null;
                var database = useDatabase ? Substitute.For<ITogglDatabase>() : null;
                var apiFactory = useApiFactory ? Substitute.For<IApiFactory>() : null;
                var timeService = useTimeService ? Substitute.For<ITimeService>() : null;
                var mailService = useMailService ? Substitute.For<IMailService>() : null;
                var ratinService = useRatingService ? Substitute.For<IRatingService>() : null;
                var googleService = useGoogleService ? Substitute.For<IGoogleService>() : null;
                var licenseProvider = useLicenseProvider ? Substitute.For<ILicenseProvider>() : null;
                var analyticsService = useAnalyticsService ? Substitute.For<IAnalyticsService>() : null;
                var stopwatchProvider = useStopwatchProvider ? Substitute.For<IStopwatchProvider>() : null;
                var backgroundService = useBackgroundService ? Substitute.For<IBackgroundService>() : null;
                var backgroundSyncService = useBackgroundSyncService ? Substitute.For<IBackgroundSyncService>() : null;
                var platformConstants = usePlatformConstants ? Substitute.For<IPlatformConstants>() : null;
                var notificationService = useNotificationService ? Substitute.For<INotificationService>() : null;
                var remoteConfigService = useRemoteConfigService ? Substitute.For<IRemoteConfigService>() : null;
                var intentDonationService = useIntentDonationService ? Substitute.For<IIntentDonationService>() : null;
                var applicationShortcutCreator = useApplicationShortcutCreator ? Substitute.For<IApplicationShortcutCreator>() : null;
                var suggestionProviderContainer = useSuggestionProviderContainer ? Substitute.For<ISuggestionProviderContainer>() : null;
                var privateSharedStorageService = usePrivateSharedStorageService ? Substitute.For<IPrivateSharedStorageService>() : null;
                var schedulerProvider = useSchedulerProvider ? Substitute.For<ISchedulerProvider>() : null;

                Action tryingToConstructWithEmptyParameters = () =>
                    TogglFoundation
                        .ForClient(agent, version)
                        .WithDatabase(database)
                        .WithScheduler(scheduler)
                        .WithApiFactory(apiFactory)
                        .WithTimeService(timeService)
                        .WithMailService(mailService)
                        .WithPlatformInfo(platformInfo)
                        .WithRatingService(ratinService)
                        .WithGoogleService(googleService)
                        .WithLicenseProvider(licenseProvider)
                        .WithAnalyticsService(analyticsService)
                        .WithStopwatchProvider(stopwatchProvider)
                        .WithBackgroundService(backgroundService)
                        .WithBackgroundSyncService(backgroundSyncService)
                        .WithSchedulerProvider(schedulerProvider)
                        .WithPlatformConstants(platformConstants)
                        .WithNotificationService(notificationService)
                        .WithRemoteConfigService(remoteConfigService)
                        .WithIntentDonationService(intentDonationService)
                        .WithApplicationShortcutCreator(applicationShortcutCreator)
                        .WithSuggestionProviderContainer(suggestionProviderContainer)
                        .WithPrivateSharedStorageService(privateSharedStorageService)
                        .Build();

                tryingToConstructWithEmptyParameters.Should().Throw<Exception>();
            }

            [Fact]
            public void BuildingWorksIfAllParametersAreProvided()
            {
                var version = Version.Parse("1.0");
                var platformInfo = new PlatformInfo();
                var scheduler = Substitute.For<IScheduler>();
                var apiFactory = Substitute.For<IApiFactory>();
                var agent = new UserAgent("Some Client", "1.0");
                var database = Substitute.For<ITogglDatabase>();
                var timeService = Substitute.For<ITimeService>();
                var mailService = Substitute.For<IMailService>();
                var ratingService = Substitute.For<IRatingService>();
                var googleService = Substitute.For<IGoogleService>();
                var licenseProvider = Substitute.For<ILicenseProvider>();
                var analyticsService = Substitute.For<IAnalyticsService>();
                var stopwatchProvider = Substitute.For<IStopwatchProvider>();
                var schedulerProvider = Substitute.For<ISchedulerProvider>();
                var platformConstants = Substitute.For<IPlatformConstants>();
                var backgroundService = Substitute.For<IBackgroundService>();
                var backgroundSyncService = Substitute.For<IBackgroundSyncService>();
                var notificationService = Substitute.For<INotificationService>();
                var remoteConfigService = Substitute.For<IRemoteConfigService>();
                var intentDonationService = Substitute.For<IIntentDonationService>();
                var applicationShortcutCreator = Substitute.For<IApplicationShortcutCreator>();
                var suggestionProviderContainer = Substitute.For<ISuggestionProviderContainer>();
                var privateSharedStorageService = Substitute.For<IPrivateSharedStorageService>();
                var automaticSyncingService = Substitute.For<IAutomaticSyncingService>();
                var rxActionFactory = Substitute.For<IRxActionFactory>();

                Action tryingToConstructWithValidParameters = () =>
                    TogglFoundation
                        .ForClient(agent, version)
                        .WithDatabase(database)
                        .WithScheduler(scheduler)
                        .WithApiFactory(apiFactory)
                        .WithTimeService(timeService)
                        .WithMailService(mailService)
                        .WithPlatformInfo(platformInfo)
                        .WithRatingService(ratingService)
                        .WithGoogleService(googleService)
                        .WithLicenseProvider(licenseProvider)
                        .WithAnalyticsService(analyticsService)
                        .WithStopwatchProvider(stopwatchProvider)
                        .WithBackgroundService(backgroundService)
                        .WithBackgroundSyncService(backgroundSyncService)
                        .WithSchedulerProvider(schedulerProvider)
                        .WithPlatformConstants(platformConstants)
                        .WithNotificationService(notificationService)
                        .WithRemoteConfigService(remoteConfigService)
                        .WithIntentDonationService(intentDonationService)
                        .WithApplicationShortcutCreator(applicationShortcutCreator)
                        .WithPrivateSharedStorageService(privateSharedStorageService)
                        .WithSuggestionProviderContainer(suggestionProviderContainer)
                        .WithAutomaticSyncingService(automaticSyncingService)
                        .Build();

                tryingToConstructWithValidParameters.Should().NotThrow();
            }
        }
    }
}
