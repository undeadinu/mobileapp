using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels.Settings;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class UpcomingEventsNotificationSettingsViewModelTests
    {
        public abstract class UpcomingEventsNotificationSettingsViewModelTest : BaseViewModelTests<UpcomingEventsNotificationSettingsViewModel>
        {
            protected override UpcomingEventsNotificationSettingsViewModel CreateViewModel()
                => new UpcomingEventsNotificationSettingsViewModel(NavigationService, UserPreferences, RxActionFactory);
        }

        public sealed class TheConstructor : UpcomingEventsNotificationSettingsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useNavigationService,
                bool useUserPreferences,
                bool useRxActionFactory)
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new UpcomingEventsNotificationSettingsViewModel(
                        useNavigationService ? NavigationService : null,
                        useUserPreferences ? UserPreferences : null,
                        useRxActionFactory ? RxActionFactory : null
                    );

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }


        public sealed class TheSelectOptionAction : UpcomingEventsNotificationSettingsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(CalendarNotificationsOption.Disabled, false, 0)]
            [InlineData(CalendarNotificationsOption.WhenEventStarts, true, 0)]
            [InlineData(CalendarNotificationsOption.FiveMinutes, true, 5)]
            [InlineData(CalendarNotificationsOption.TenMinutes, true, 10)]
            [InlineData(CalendarNotificationsOption.FifteenMinutes, true, 15)]
            [InlineData(CalendarNotificationsOption.ThirtyMinutes, true, 30)]
            [InlineData(CalendarNotificationsOption.OneHour, true, 60)]
            public async Task SavesTheSelectedOption(CalendarNotificationsOption option, bool enabled, int minutes)
            {
                ViewModel.SelectOption.Execute(option);

                TestScheduler.Start();
                UserPreferences.Received().SetCalendarNotificationsEnabled(enabled);

                if (enabled)
                    UserPreferences.Received().SetTimeSpanBeforeCalendarNotifications(Arg.Is<TimeSpan>(arg => arg == TimeSpan.FromMinutes(minutes)));
                else
                    UserPreferences.DidNotReceive().SetTimeSpanBeforeCalendarNotifications(Arg.Any<TimeSpan>());

                await NavigationService.Received().Close(Arg.Any<UpcomingEventsNotificationSettingsViewModel>(), Unit.Default);
            }
        }
    }
}
