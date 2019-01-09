using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Extensions;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac.Extensions;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class OnboardingViewModelTests
    {
        public abstract class OnboardingViewModelTest : BaseViewModelTests<OnboardingViewModel>
        {
            protected override OnboardingViewModel CreateViewModel()
                => new OnboardingViewModel(
                    NavigationService,
                    OnboardingStorage,
                    AnalyticsService,
                    RxActionFactory,
                    SchedulerProvider);
        }

        public sealed class TheConstructor : OnboardingViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useNavigationService,
                bool useOnboardingStorage,
                bool useAnalyticsService,
                bool useRxActionFactory,
                bool useSchedulerProvider)
            {
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new OnboardingViewModel(navigationService, onboardingStorage, analyticsService,
                        rxActionFactory, schedulerProvider);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheCurrentPageProperty : OnboardingViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task DoesNotAllowUsersToSetItsValueToAValueGreaterThanTheNumberOfPagesMinusOne()
            {
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<int>();
                ViewModel.CurrentPage.Subscribe(observer);

                await ViewModel.ChangePage(int.MaxValue);
                TestScheduler.Start();

                observer.LastValue().Should().Be(OnboardingViewModel.ReportsPage);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotAllowUsersToSetItsValueToANegativeNumber()
            {
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<int>();
                ViewModel.CurrentPage.Subscribe(observer);

                await ViewModel.ChangePage(-1);
                TestScheduler.Start();

                observer.LastValue().Should().Be(OnboardingViewModel.TrackPage);
            }
        }

        public sealed class TheIsFirstPageProperty : OnboardingViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(OnboardingViewModel.TrackPage)]
            [InlineData(OnboardingViewModel.MostUsedPage)]
            [InlineData(OnboardingViewModel.ReportsPage)]
            public async Task OnlyReturnsTrueForTheFirstPage(int page)
            {
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsFirstPage.Subscribe(observer);

                await ViewModel.ChangePage(page);
                TestScheduler.Start();

                var expected = page == 0;
                observer.LastValue().Should().Be(expected);
            }
        }

        public sealed class TheIsLastPageProperty : OnboardingViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(OnboardingViewModel.TrackPage)]
            [InlineData(OnboardingViewModel.MostUsedPage)]
            [InlineData(OnboardingViewModel.ReportsPage)]
            public async Task OnlyReturnsTrueInThePageWhoseIndexEqualsToTheNumberOfPagesMinusOne(int page)
            {
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsLastPage.Subscribe(observer);

                await ViewModel.ChangePage(page);
                TestScheduler.Start();

                var expected = page == ViewModel.NumberOfPages - 1;
                observer.LastValue().Should().Be(expected);
            }
        }

        public sealed class TheSkipAction : OnboardingViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheLoginPage()
            {
                ViewModel.SkipOnboarding.Execute();

                await NavigationService.Received().Navigate<LoginViewModel>();
            }

            [Theory, LogIfTooSlow]
            [InlineData(OnboardingViewModel.TrackPage, nameof(OnboardingViewModel.TrackPage))]
            [InlineData(OnboardingViewModel.MostUsedPage, nameof(OnboardingViewModel.MostUsedPage))]
            [InlineData(OnboardingViewModel.ReportsPage, nameof(OnboardingViewModel.ReportsPage))]
            public async Task CallsTheAnalyticsServiceIndicatingTheCurrentPage(int page, string expectedPageName)
            {
                await ViewModel.Initialize();

                await ViewModel.ChangePage(page);
                TestScheduler.Start();

                ViewModel.SkipOnboarding.Execute();

                AnalyticsService.Received().OnboardingSkip.Track(expectedPageName);
            }
        }

        public sealed class TheNextAction : OnboardingViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(OnboardingViewModel.TrackPage, OnboardingViewModel.MostUsedPage)]
            [InlineData(OnboardingViewModel.MostUsedPage, OnboardingViewModel.ReportsPage)]
            public async Task AdvancesToTheNextPage(int from, int to)
            {
                await ViewModel.Initialize();
                await ViewModel.ChangePage(from);
                var observer = TestScheduler.CreateObserver<int>();
                ViewModel.CurrentPage.Subscribe(observer);

                ViewModel.GoToNextPage.Execute();
                TestScheduler.Start();

                observer.LastValue().Should().Be(to);
            }

            [Fact, LogIfTooSlow]
            public async Task CompletesOnboardingWhenOnTheLastPage()
            {
                await ViewModel.Initialize();
                await ViewModel.ChangePage(OnboardingViewModel.ReportsPage);
                var observer = TestScheduler.CreateObserver<int>();
                ViewModel.CurrentPage.Subscribe(observer);

                ViewModel.GoToNextPage.Execute();
                TestScheduler.Start();

                await NavigationService.Received().Navigate<LoginViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheCompletedOnboardingFlagWhenUserViewsAllOnboardingPagesAndTapsOnNext()
            {
                await ViewModel.Initialize();
                await ViewModel.ChangePage(OnboardingViewModel.TrackPage);
                var observer = TestScheduler.CreateObserver<int>();
                ViewModel.CurrentPage.Subscribe(observer);

                for (int i = 0; i < ViewModel.NumberOfPages; ++i)
                {
                    await ViewModel.ChangePage(i + 1);
                }
                TestScheduler.Start();

                OnboardingStorage.Received().SetCompletedOnboarding();
            }
        }

        public sealed class ThePreviousAction : OnboardingViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(OnboardingViewModel.MostUsedPage, OnboardingViewModel.TrackPage)]
            [InlineData(OnboardingViewModel.ReportsPage, OnboardingViewModel.MostUsedPage)]
            public async Task ReturnsToThePreviousPage(int from, int to)
            {
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<int>();
                ViewModel.CurrentPage.Subscribe(observer);
                await ViewModel.ChangePage(from);

                ViewModel.GoToPreviousPage.Execute();
                TestScheduler.Start();

                observer.LastValue().Should().Be(to);
            }

            [Theory, LogIfTooSlow]
            [InlineData(OnboardingViewModel.TrackPage, false)]
            [InlineData(OnboardingViewModel.MostUsedPage, true)]
            [InlineData(OnboardingViewModel.ReportsPage, true)]
            public async Task ShouldBeDisableWhenInFirstPage(int page, bool shouldBeEnabled)
            {
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.GoToPreviousPage.Enabled.Subscribe(observer);
                await ViewModel.ChangePage(page);

                TestScheduler.Start();

                observer.LastValue().Should().Be(shouldBeEnabled);
            }
        }

        public sealed class TheInitializeMethod : OnboardingViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task InitializesCurrentPageToTrackPageIfUserHasNotCompletedOnboarding()
            {
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<int>();
                ViewModel.CurrentPage.Subscribe(observer);

                TestScheduler.Start();

                observer.LastValue().Should().Be(OnboardingViewModel.TrackPage);
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheLoginPageIfUserHasCompletedOnboarding()
            {
                OnboardingStorage.CompletedOnboarding().Returns(true);

                await ViewModel.Initialize();

                await NavigationService.Received().Navigate<LoginViewModel>();
            }
        }
    }
}
