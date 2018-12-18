using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels.Settings
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarSettingsViewModel : SelectUserCalendarsViewModelBase
    {
        private readonly IPermissionsService permissionsService;
        private readonly IUserPreferences userPreferences;
        private readonly IRxActionFactory rxActionFactory;

        public bool PermissionGranted { get; private set; }

        public UIAction RequestAccess { get; }

        public CalendarSettingsViewModel(
            IUserPreferences userPreferences,
            IInteractorFactory interactorFactory,
            IPermissionsService permissionsService,
            IRxActionFactory rxActionFactory)
            : base(userPreferences, interactorFactory, rxActionFactory)
        {
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));

            this.userPreferences = userPreferences;
            this.permissionsService = permissionsService;

            RequestAccess = rxActionFactory.FromAction(requestAccess);

            SelectCalendar
                .Elements
                .Subscribe(onCalendarSelected);
        }

        public async override Task Initialize()
        {
            PermissionGranted = await permissionsService.CalendarPermissionGranted;

            await base.Initialize();
        }

        private void requestAccess()
        {
            permissionsService.OpenAppSettings();
        }

        private void onCalendarSelected()
        {
            userPreferences.SetEnabledCalendars(SelectedCalendarIds.ToArray());
        }
    }
}
