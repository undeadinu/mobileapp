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
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Multivac.Extensions;
using UIKit;
using Math = System.Math;

namespace Toggl.Daneel.ViewControllers
{
    [TabPresentation]
    public sealed partial class CalendarViewController : ReactiveViewController<CalendarViewModel>
    {
        private readonly UIImageView titleImage = new UIImageView(UIImage.FromBundle("togglLogo"));
        private readonly ITimeService timeService;

        private CalendarCollectionViewLayout layout;
        private CalendarCollectionViewSource dataSource;
        private CalendarCollectionViewEditItemHelper editItemHelper;
        private CalendarCollectionViewCreateFromSpanHelper createFromSpanHelper;

        private int workingHoursStart;
        private int workingHoursEnd;

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

            ViewModel.SettingsAreVisible
                .Subscribe(settingsButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            createFromSpanHelper.CreateFromSpan
                .Subscribe(ViewModel.OnDurationSelected.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.WorkingHoursStart
                .Subscribe(start =>
                {
                    workingHoursStart = start;
                    updateScrollOffset();
                })
                .DisposedBy(DisposeBag);

            ViewModel.WorkingHoursEnd
                .Subscribe(end =>
                {
                    workingHoursEnd = end;
                    updateScrollOffset();
                })
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
            updateScrollOffset();
        }

        private void updateScrollOffset()
        {
            selectGoodScrollPoint(timeService.CurrentDateTime.LocalDateTime.TimeOfDay);
        }

        private void selectGoodScrollPoint(TimeSpan timeOfDay)
        {
            var frameHeight =
                CalendarCollectionView.Frame.Height
                    - CalendarCollectionView.ContentInset.Top
                    - CalendarCollectionView.ContentInset.Bottom;
            var centeredHour = calculateCenteredHour(timeOfDay.TotalHours, frameHeight);
            Console.WriteLine($@"Current time: ${timeOfDay} Centered hour: ${centeredHour}");

            var centeredHourY = (centeredHour / 24) * CalendarCollectionView.ContentSize.Height;
            var scrollPointY = centeredHourY - frameHeight / 2;
            var scrollPoint = new CGPoint(0, scrollPointY.Clamp(0, CalendarCollectionView.ContentSize.Height));

            CalendarCollectionView.SetContentOffset(scrollPoint, false);
        }

        private double calculateCenteredHour(double currentHour, double frameHeight)
        {
            var hoursPerHalfOfScreen = (frameHeight / (CalendarCollectionView.ContentSize.Height / 24)) / 2;

            if (currentHour < workingHoursStart)
            {
                return currentHour - 1 + hoursPerHalfOfScreen;
            }

            if (currentHour > workingHoursEnd)
            {
                return currentHour + 1 - hoursPerHalfOfScreen;
            }

            var naiveStart = currentHour - hoursPerHalfOfScreen;
            var naiveEnd = currentHour + hoursPerHalfOfScreen;

            if (naiveStart >= workingHoursStart && workingHoursEnd < naiveEnd)
            {
                var start = naiveStart - Math.Min(naiveStart - workingHoursStart, naiveEnd - workingHoursEnd);
                return start + hoursPerHalfOfScreen;
            }

            if (naiveStart < workingHoursStart && workingHoursEnd >= naiveEnd)
            {
                var end = naiveEnd + Math.Min(workingHoursEnd - naiveEnd, workingHoursStart - naiveStart);
                return end - hoursPerHalfOfScreen;
            }

            return currentHour;
        }
    }
}
