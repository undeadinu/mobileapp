using System;
using System.Reactive.Linq;
using CoreGraphics;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers.Calendar
{
    [ModalDialogPresentation]
    public sealed partial class SelectUserCalendarsViewController
        : ReactiveViewController<SelectUserCalendarsViewModel>
    {
        private const int heightAboveTableView = 98;
        private const int heightBelowTableView = 80;
        private readonly int maxHeight = UIScreen.MainScreen.Bounds.Width > 320 ? 627 : 528;
        private const int width = 288;

        private const float enabledDoneButtonAlpha = 1;
        private const float disabledDoneButtonAlpha = 0.32f;

        public SelectUserCalendarsViewController() : base(null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            HeadingLabel.Text = Resources.SelectCalendars;
            DescriptionLabel.Text = Resources.SelectCalendarsDescription;
            DoneButton.SetTitle(Resources.Done, UIControlState.Normal);

            var source = new SelectUserCalendarsTableViewSource(TableView, ViewModel.Calendars);
            TableView.Source = source;

            DoneButton.Rx()
                .BindAction(ViewModel.Done)
                .DisposedBy(DisposeBag);

            source.ItemSelected
                .Subscribe(ViewModel.SelectCalendar.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.Done.Enabled
                .Subscribe(DoneButton.Rx().Enabled())
                .DisposedBy(DisposeBag);

            ViewModel.Done.Enabled
                .Select(alphaForEnabled)
                .Subscribe(DoneButton.Rx().AnimatedAlpha())
                .DisposedBy(DisposeBag);
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            setDialogSize();
        }

        private void setDialogSize()
        {
            var targetHeight = calculateTargetHeight();
            PreferredContentSize = new CGSize(
                width,
                targetHeight > maxHeight ? maxHeight : targetHeight
            );

            //Implementation in ModalPresentationController
            View.Frame = PresentationController.FrameOfPresentedViewInContainerView;

            TableView.ScrollEnabled = targetHeight > maxHeight;
        }

        private int calculateTargetHeight()
            => heightAboveTableView + heightBelowTableView + (int)TableView.ContentSize.Height;

        private float alphaForEnabled(bool enabled)
            => enabled ? enabledDoneButtonAlpha : disabledDoneButtonAlpha;
    }
}
