using System;
using System.Reactive.Linq;
using CoreGraphics;
using MvvmCross;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.Views.Calendar;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [TabPresentation]
    public sealed partial class CalendarViewController : ReactiveViewController<CalendarViewModel>
    {
        private const double minimumOffsetOfCurrentTimeIndicatorFromScreenEdge = 0.2;
        private const double middleOfTheDay = 12;

        private readonly UIImageView titleImage = new UIImageView(UIImage.FromBundle("togglLogo"));
        private readonly ITimeService timeService;

        private CalendarCollectionViewLayout layout;
        private CalendarCollectionViewSource dataSource;
        private CalendarCollectionViewEditItemHelper editItemHelper;
        private CalendarCollectionViewCreateFromSpanHelper createFromSpanHelper;

        private readonly UIButton settingsButton = new UIButton(new CGRect(0, 0, 40, 50));

        public CalendarViewController()
            : base(nameof(CalendarViewController))
        {
            timeService = Mvx.Resolve<ITimeService>();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            settingsButton.SetImage(UIImage.FromBundle("icSettings"), UIControlState.Normal);

            ViewModel
                .ShouldShowOnboarding
                .FirstAsync()
                .Subscribe(
                    shouldShowOnboarding => OnboardingView.Alpha = shouldShowOnboarding ? 1: 0)
                .DisposedBy(DisposeBag);

            ViewModel.ShouldShowOnboarding
                .Subscribe(OnboardingView.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);

            GetStartedButton.Rx()
                .BindAction(ViewModel.GetStarted)
                .DisposedBy(DisposeBag);

            dataSource = new CalendarCollectionViewSource(
                timeService,
                CalendarCollectionView,
                ViewModel.TimeOfDayFormat,
                ViewModel.CalendarItems);

            layout = new CalendarCollectionViewLayout(timeService, dataSource);

            editItemHelper = new CalendarCollectionViewEditItemHelper(CalendarCollectionView, timeService, dataSource, layout);
            createFromSpanHelper = new CalendarCollectionViewCreateFromSpanHelper(CalendarCollectionView, dataSource, layout);

            CalendarCollectionView.SetCollectionViewLayout(layout, false);
            CalendarCollectionView.Delegate = dataSource;
            CalendarCollectionView.DataSource = dataSource;
            CalendarCollectionView.ContentInset = new UIEdgeInsets(20, 0, 20, 0);

            dataSource.ItemTapped
                .Subscribe(ViewModel.OnItemTapped.Inputs)
                .DisposedBy(DisposeBag);

            settingsButton.Rx()
                .BindAction(ViewModel.SelectCalendars)
                .DisposedBy(DisposeBag);

            editItemHelper.EditCalendarItem
                .Subscribe(ViewModel.OnUpdateTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            editItemHelper.LongPressCalendarEvent
                .Subscribe(ViewModel.OnCalendarEventLongPressed.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.SettingsAreVisible
                .Subscribe(settingsButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            createFromSpanHelper.CreateFromSpan
                .Subscribe(ViewModel.OnDurationSelected.Inputs)
                .DisposedBy(DisposeBag);

            CalendarCollectionView.LayoutIfNeeded();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            NavigationItem.TitleView = titleImage;
            NavigationItem.RightBarButtonItems = new[]
            {
                new UIBarButtonItem(settingsButton)
            };

            layout.InvalidateCurrentTimeLayout();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            if (CalendarCollectionView.ContentSize.Height == 0)
                return;

            selectGoodScrollPoint(timeService.CurrentDateTime.LocalDateTime.TimeOfDay);
        }

        private void selectGoodScrollPoint(TimeSpan timeOfDay)
        {
            var frameHeight =
                CalendarCollectionView.Frame.Height
                    - CalendarCollectionView.ContentInset.Top
                    - CalendarCollectionView.ContentInset.Bottom;
            var hoursOnScreen = frameHeight / (CalendarCollectionView.ContentSize.Height / 24);
            var centeredHour = calculateCenteredHour(timeOfDay.TotalHours, hoursOnScreen);

            var offsetY = (centeredHour / 24) * CalendarCollectionView.ContentSize.Height - (frameHeight / 2);
            var scrollPointY = offsetY.Clamp(0, CalendarCollectionView.ContentSize.Height - frameHeight);
            var offset = new CGPoint(0, scrollPointY);
            CalendarCollectionView.SetContentOffset(offset, false);
        }

        private static double calculateCenteredHour(double currentHour, double hoursOnScreen)
        {
            var hoursPerHalfOfScreen = hoursOnScreen / 2;
            var minimumOffset = hoursOnScreen * minimumOffsetOfCurrentTimeIndicatorFromScreenEdge;

            var center = (currentHour + middleOfTheDay) / 2;

            if (currentHour < center - hoursPerHalfOfScreen + minimumOffset)
            {
                // the current time indicator would be too close to the top edge of the screen
                return currentHour - minimumOffset + hoursPerHalfOfScreen;
            }

            if (currentHour > center + hoursPerHalfOfScreen - minimumOffset)
            {
                // the current time indicator would be too close to the bottom edge of the screen
                return currentHour + minimumOffset - hoursPerHalfOfScreen;
            }

            return center;
        }
    }
}
