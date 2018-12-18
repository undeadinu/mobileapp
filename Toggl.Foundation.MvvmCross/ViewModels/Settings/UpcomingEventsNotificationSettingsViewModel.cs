using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels.Settings
{
    [Preserve(AllMembers = true)]
    public sealed class UpcomingEventsNotificationSettingsViewModel : MvxViewModelResult<Unit>
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IUserPreferences userPreferences;
        private readonly IRxActionFactory rxActionFactory;

        public int SelectedOptionIndex { get; private set; }

        public IList<CalendarNotificationsOption> AvailableOptions { get; }

        public InputAction<CalendarNotificationsOption> SelectOption { get; }
        public UIAction Close { get; }

        public UpcomingEventsNotificationSettingsViewModel(
            IMvxNavigationService navigationService,
            IUserPreferences userPreferences,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.navigationService = navigationService;
            this.userPreferences = userPreferences;
            this.rxActionFactory = rxActionFactory;

            this.AvailableOptions = new List<CalendarNotificationsOption>
            {
                CalendarNotificationsOption.Disabled,
                CalendarNotificationsOption.WhenEventStarts,
                CalendarNotificationsOption.FiveMinutes,
                CalendarNotificationsOption.TenMinutes,
                CalendarNotificationsOption.FifteenMinutes,
                CalendarNotificationsOption.ThirtyMinutes,
                CalendarNotificationsOption.OneHour
            };

            SelectOption = rxActionFactory.FromAction<CalendarNotificationsOption>(onSelectOption);
            Close = rxActionFactory.FromAsync(() => navigationService.Close(this, Unit.Default));
        }

        public override async Task Initialize()
        {
            await base.Initialize();
            var selectedOption = await userPreferences.CalendarNotificationsSettings().FirstAsync();
            SelectedOptionIndex = AvailableOptions.IndexOf(selectedOption);
        }

        private void onSelectOption(CalendarNotificationsOption option)
        {
            var enabled = option != CalendarNotificationsOption.Disabled;

            userPreferences.SetCalendarNotificationsEnabled(enabled);

            if (enabled)
                userPreferences.SetTimeSpanBeforeCalendarNotifications(option.Duration());

            navigationService.Close(this, Unit.Default);
        }
    }
}
