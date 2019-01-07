using Toggl.Daneel.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Daneel.Presentation.Attributes;
using CoreGraphics;
using UIKit;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Multivac.Extensions;
using Toggl.Foundation.MvvmCross.Extensions;
using System.Reactive;
using Foundation;
using Toggl.Foundation;

namespace Toggl.Daneel.ViewControllers.Calendar
{
    [ModalDialogPresentation]
    public sealed partial class CalendarPermissionDeniedViewController
        : ReactiveViewController<CalendarPermissionDeniedViewModel>
    {
        private const float cardHeight = 342;

        public CalendarPermissionDeniedViewController() : base(null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            HeadingLabel.Text = Resources.NoWorries;
            MessageLabel.Text = Resources.EnableAccessLater;
            ContinueWithoutAccessButton.SetTitle(Resources.ContinueWithoutAccess, UIControlState.Normal);

            var screenWidth = UIScreen.MainScreen.Bounds.Width;
            PreferredContentSize = new CGSize
            {
                // ScreenWidth - 32 for 16pt margins on both sides
                Width = screenWidth > 320 ? screenWidth - 32 : 312,
                Height = cardHeight
            };

            EnableAccessButton.Rx()
                .BindAction(ViewModel.EnableAccess)
                .DisposedBy(DisposeBag);

            ContinueWithoutAccessButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);
        }
    }
}

