using System.Reactive;
using CoreGraphics;
using Foundation;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Settings;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers.Settings
{
    [ModalCardPresentation]
    public sealed partial class UpcomingEventsNotificationSettingsViewController : ReactiveViewController<UpcomingEventsNotificationSettingsViewModel>
    {
        public UpcomingEventsNotificationSettingsViewController() : base(nameof(UpcomingEventsNotificationSettingsViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            TitleLabel.Text = Resources.UpcomingEvent;

            var dataSource = new UpcomingEventsNotificationSettingsSource(TableView, ViewModel.AvailableOptions);

            TableView.ScrollEnabled = false;
            TableView.TableFooterView = new UIView(CGRect.Empty);
            TableView.Source = dataSource;
            TableView.SelectRow(NSIndexPath.FromRowSection(ViewModel.SelectedOptionIndex, 0), false, UITableViewScrollPosition.None);

            dataSource.SelectedOptionChanged
                .Subscribe(ViewModel.SelectOption.Inputs)
                .DisposedBy(DisposeBag);

            CloseButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);
        }
    }
}
