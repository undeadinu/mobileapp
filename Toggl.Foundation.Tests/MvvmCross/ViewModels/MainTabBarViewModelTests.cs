using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public class MainTabViewModelTests
    {
        public abstract class MainTabViewModelTest : BaseViewModelTests<MainTabBarViewModel>
        {
            protected override MainTabBarViewModel CreateViewModel()
                => new MainTabBarViewModel(
                    TimeService,
                    DataSource,
                    DialogService,
                    RatingService,
                    UserPreferences,
                    AnalyticsService,
                    BackgroundService,
                    InteractorFactory,
                    OnboardingStorage,
                    SchedulerProvider,
                    PermissionsService,
                    NavigationService,
                    RemoteConfigService,
                    SuggestionProviderContainer,
                    IntentDonationService,
                    AccessRestrictionStorage,
                    StopwatchProvider,
                    RxActionFactory
                );

            protected override void AdditionalViewModelSetup()
            {
                base.AdditionalViewModelSetup();
                var provider = Substitute.For<ISuggestionProvider>();
                provider.GetSuggestions().Returns(Observable.Empty<Suggestion>());
                SuggestionProviderContainer.Providers.Returns(new[] { provider }.ToList().AsReadOnly());
            }
        }

        public sealed class TheConstructor : MainTabViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                    bool useTimeService,
                    bool useDataSource,
                    bool useDialogService,
                    bool useRatingService,
                    bool useUserPreferences,
                    bool useAnalyticsService,
                    bool useBackgroundService,
                    bool useInteractorFactory,
                    bool useOnboardingStorage,
                    bool useSchedulerProvider,
                    bool usePermissionsService,
                    bool useNavigationService,
                    bool useRemoteConfigService,
                    bool useIntentDonationService,
                    bool useAccessRestrictionStorage,
                    bool useSuggestionProviderContainer,
                    bool useStopwatchProvider,
                    bool useRxActionFactory)

            {
                var timeService = useTimeService ? TimeService : null;
                var dataSource = useDataSource ? DataSource : null;
                var dialogService = useDialogService ? DialogService : null;
                var ratingService = useRatingService ? RatingService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var backgroundService = useBackgroundService ? BackgroundService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var permissionsService = usePermissionsService ? PermissionsService : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var remoteConfigService = useRemoteConfigService ? RemoteConfigService : null;
                var accessRestrictionStorage = useAccessRestrictionStorage ? AccessRestrictionStorage : null;
                var suggestionProviderContainer = useSuggestionProviderContainer ? SuggestionProviderContainer : null;
                var intentDonationService = useIntentDonationService ? IntentDonationService : null;
                var stopwatchProvider = useStopwatchProvider ? StopwatchProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new MainTabBarViewModel(
                        timeService,
                        dataSource,
                        dialogService,
                        ratingService,
                        userPreferences,
                        analyticsService,
                        backgroundService,
                        interactorFactory,
                        onboardingStorage,
                        schedulerProvider,
                        permissionsService,
                        navigationService,
                        remoteConfigService,
                        suggestionProviderContainer,
                        intentDonationService,
                        accessRestrictionStorage,
                        stopwatchProvider,
                        rxActionFactory
                    );

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheTabsProperty : MainTabViewModelTest
        {
            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ShouldConditionallyIncludeTheCalendarViewModelBasedOntheRemoteConfigService(bool enabled)
            {
                var expectedTabCount = 2 + (enabled ? 1 : 0);
                RemoteConfigService
                    .IsCalendarFeatureEnabled
                    .Returns(Observable.Return(enabled));

                await ViewModel.Initialize();

                ViewModel.Tabs.Count().Should().Be(expectedTabCount);
            }
        }
    }
}
