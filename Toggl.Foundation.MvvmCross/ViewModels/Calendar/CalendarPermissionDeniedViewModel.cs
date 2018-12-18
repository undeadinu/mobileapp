using System.Reactive;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarPermissionDeniedViewModel : MvxViewModelResult<Unit>
    {
        private readonly IPermissionsService permissionsService;
        private readonly IRxActionFactory rxActionFactory;

        public UIAction EnableAccess { get; }
        public UIAction Close { get; }

        public CalendarPermissionDeniedViewModel(IPermissionsService permissionsService, IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.permissionsService = permissionsService;

            EnableAccess = rxActionFactory.FromAction(enableAccess);
            Close = rxActionFactory.FromAsync(() => NavigationService.Close(this, Unit.Default));
        }

        private void enableAccess()
        {
            permissionsService.OpenAppSettings();
        }
    }
}
