using MvvmCross.Platforms.Ios.Presenters.Attributes;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [MvxRootPresentation]
    public sealed partial class OutdatedAppViewController : ReactiveViewController<OutdatedAppViewModel>
    {
        public OutdatedAppViewController()
            : base(nameof(OutdatedAppViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            HeadingLabel.Text = Resources.Oops;
            TextLabel.Text = Resources.AppOutdatedMessage;
            UpdateButton.SetTitle(Resources.UpdateTheApp, UIControlState.Normal);

            UpdateButton.Rx()
                .BindAction(ViewModel.UpdateApp)
                .DisposedBy(DisposeBag);

            WebsiteButton.Rx()
                .BindAction(ViewModel.OpenWebsite)
                .DisposedBy(DisposeBag);
        }
    }
}
