using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using MvvmCross.Base;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Views;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [NestedPresentation]
    public partial class ReportsCalendarViewController : MvxViewController<ReportsCalendarViewModel>, IUICollectionViewDelegate
    {
        private bool calendarInitialized;
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();
        private ReportsCalendarCollectionViewSource calendarCollectionViewSource;
        private List<ReportsCalendarPageViewModel> pendingMonthsUpdate;

        public ReportsCalendarViewController()
            : base(nameof(ReportsCalendarViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            calendarCollectionViewSource = new ReportsCalendarCollectionViewSource(CalendarCollectionView);
            var calendarCollectionViewLayout = new ReportsCalendarCollectionViewLayout();
            CalendarCollectionView.Delegate = calendarCollectionViewSource;
            CalendarCollectionView.DataSource = calendarCollectionViewSource;
            CalendarCollectionView.CollectionViewLayout = calendarCollectionViewLayout;

            var quickSelectCollectionViewSource = new ReportsCalendarQuickSelectCollectionViewSource(QuickSelectCollectionView);
            QuickSelectCollectionView.Source = quickSelectCollectionViewSource;

            ViewModel.DayHeadersObservable
                .Subscribe(setupDayHeaders)
                .DisposedBy(disposeBag);

            ViewModel.MonthsObservable
                .Subscribe(calendarCollectionViewSource.UpdateMonths)
                .DisposedBy(disposeBag);

            calendarCollectionViewSource.DayTaps
                .Subscribe(ViewModel.SelectDay.Inputs)
                .DisposedBy(disposeBag);

            ViewModel.HighlightedDateRangeObservable
                .Subscribe(calendarCollectionViewSource.UpdateSelection)
                .DisposedBy(disposeBag);

            ViewModel.CurrentMonthObservable
                .Select(month => month.Year.ToString())
                .Subscribe(CurrentYearLabel.Rx().Text())
                .DisposedBy(disposeBag);

            ViewModel.CurrentMonthObservable
                .Select(month => month.Month)
                .Select(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName)
                .Subscribe(CurrentMonthLabel.Rx().Text())
                .DisposedBy(disposeBag);

            ViewModel.SelectedDateRangeObservable
                .Subscribe(quickSelectCollectionViewSource.UpdateSelection)
                .DisposedBy(disposeBag);

            quickSelectCollectionViewSource.ShortcutTaps
                .Subscribe(ViewModel.SelectShortcut.Inputs)
                .DisposedBy(disposeBag);

            ViewModel.QuickSelectShortcutsObservable
                .Subscribe(quickSelectCollectionViewSource.UpdateShortcuts)
                .DisposedBy(disposeBag);
        }

        public override void DidMoveToParentViewController(UIViewController parent)
        {
            base.DidMoveToParentViewController(parent);

            //The constraint isn't available before DidMoveToParentViewController

            var rowHeight = ReportsCalendarCollectionViewLayout.CellHeight;
            var additionalHeight = View.Bounds.Height - CalendarCollectionView.Bounds.Height;

            var heightConstraint = View
                .Superview
                .Constraints
                .Single(c => c.FirstAttribute == NSLayoutAttribute.Height);

            ViewModel.RowsInCurrentMonthObservable
                .Select(rows => rows * rowHeight + additionalHeight)
                .Subscribe(heightConstraint.Rx().ConstantAnimated())
                .DisposedBy(disposeBag);
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (calendarInitialized) return;

            calendarCollectionViewSource.DecelerationEndedObservable
                .Subscribe(ViewModel.SetCurrentPage)
                .DisposedBy(disposeBag);

            ViewModel.CurrentPageObservable
                .Subscribe(CalendarCollectionView.Rx().CurrentPageObserver())
                .DisposedBy(disposeBag);

            calendarInitialized = true;
        }

        private void setupDayHeaders(IReadOnlyList<string> dayHeaders)
        {
            DayHeader0.Text = dayHeaders[0];
            DayHeader1.Text = dayHeaders[1];
            DayHeader2.Text = dayHeaders[2];
            DayHeader3.Text = dayHeaders[3];
            DayHeader4.Text = dayHeaders[4];
            DayHeader5.Text = dayHeaders[5];
            DayHeader6.Text = dayHeaders[6];
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                disposeBag.Dispose();
            }
        }
    }
}

