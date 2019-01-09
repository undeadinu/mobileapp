using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using CoreGraphics;
using Foundation;
using MvvmCross.Binding.BindingContext;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using Toggl.Multivac.Extensions;
using UIKit;
using static Toggl.Daneel.Extensions.AnimationExtensions;

namespace Toggl.Daneel.ViewControllers
{
    [TabPresentation]
    public sealed partial class ReportsViewController : ReactiveViewController<ReportsViewModel>
    {
        private const string boundsKey = "bounds";

        private const double maximumWorkspaceNameLabelWidth = 144;

        private nfloat calendarHeight => CalendarContainer.Bounds.Height;

        private UIButton titleButton;

        private ReportsTableViewSource source;

        private IDisposable calendarSizeDisposable;

        internal UIView CalendarContainerView => CalendarContainer;

        internal bool CalendarIsVisible { get; private set; }

        public ReportsViewController() : base(nameof(ReportsViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            prepareViews();

            calendarSizeDisposable = CalendarContainer.AddObserver(boundsKey, NSKeyValueObservingOptions.New, onCalendarSizeChanged);

            source = new ReportsTableViewSource(ReportsTableView, ViewModel);
            source.OnScroll += onReportsTableScrolled;
            ReportsTableView.Source = source;

            bool areThereEnoughWorkspaces(ICollection<(string ItemName, IThreadSafeWorkspace Item)> workspaces) => workspaces.Count > 1;

            bool isWorkspaceNameTooLong(string workspaceName)
            {
                var attributes = new UIStringAttributes { Font = WorkspaceLabel.Font };
                var size = new NSString(workspaceName).GetSizeUsingAttributes(attributes);
                return size.Width >= maximumWorkspaceNameLabelWidth;
            };

            //Text
            ViewModel.WorkspaceNameObservable
                .Subscribe(WorkspaceLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.CurrentDateRangeStringObservable
                .Subscribe(titleButton.Rx().Title())
                .DisposedBy(DisposeBag);

            //Visibility
            ViewModel.WorkspacesObservable
                .Select(areThereEnoughWorkspaces)
                .Subscribe(WorkspaceButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.WorkspaceNameObservable
                .Select(isWorkspaceNameTooLong)
                .Subscribe(WorkspaceFadeView.Rx().FadeRight())
                .DisposedBy(DisposeBag);

            //Commands
            titleButton.Rx().Tap()
                .Subscribe(ViewModel.ToggleCalendar)
                .DisposedBy(DisposeBag);

            ReportsTableView.Rx().Tap()
                .Subscribe(ViewModel.HideCalendar)
                .DisposedBy(DisposeBag);

            WorkspaceButton.Rx()
                .BindAction(ViewModel.SelectWorkspace)
                .DisposedBy(DisposeBag);

            ViewModel.SegmentsObservable
                .Subscribe(segments => source.ItemsSource = segments)
                .DisposedBy(DisposeBag);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            source.OnScroll -= onReportsTableScrolled;

            calendarSizeDisposable?.Dispose();
            calendarSizeDisposable = null;
        }

        private void onReportsTableScrolled(object sender, CGPoint offset)
        {
            if (CalendarIsVisible)
            {
                var topConstant = (TopCalendarConstraint.Constant + offset.Y).Clamp(0, calendarHeight);
                TopCalendarConstraint.Constant = topConstant;

                if (topConstant == 0) return;

                // we need to adjust the offset of the scroll view so that it doesn't fold
                // under the calendar while scrolling up
                var adjustedOffset = new CGPoint(offset.X, ReportsTableView.ContentOffset.Y - offset.Y);
                ReportsTableView.SetContentOffset(adjustedOffset, false);
                View.LayoutIfNeeded();

                if (topConstant == calendarHeight)
                {
                    HideCalendar();
                }
            }
        }

        internal void ShowCalendar()
        {
            TopCalendarConstraint.Constant = 0;
            Animate(
                Animation.Timings.EnterTiming,
                Animation.Curves.SharpCurve,
                () => View.LayoutSubviews(),
                () => CalendarIsVisible = true);
        }

        internal void HideCalendar()
        {
            TopCalendarConstraint.Constant = calendarHeight;
            Animate(
                Animation.Timings.EnterTiming,
                Animation.Curves.SharpCurve,
                () => View.LayoutSubviews(),
                () => CalendarIsVisible = false);
        }

        private void prepareViews()
        {
            // Title view
            NavigationItem.TitleView = titleButton = new UIButton(new CGRect(0, 0, 200, 40));
            titleButton.Font = UIFont.SystemFontOfSize(14, UIFontWeight.Medium);
            titleButton.SetTitleColor(UIColor.Black, UIControlState.Normal);

            // Calendar configuration
            TopCalendarConstraint.Constant = calendarHeight;

            // Workspace button settings
            WorkspaceFadeView.FadeWidth = 32;
            WorkspaceButton.Layer.ShadowColor = UIColor.Black.CGColor;
            WorkspaceButton.Layer.ShadowRadius = 10;
            WorkspaceButton.Layer.ShadowOffset = new CGSize(0, 2);
            WorkspaceButton.Layer.ShadowOpacity = 0.10f;
        }

        private void onCalendarSizeChanged(NSObservedChange change)
        {
            TopCalendarConstraint.Constant = CalendarIsVisible ? 0 : calendarHeight;
        }
    }
}

