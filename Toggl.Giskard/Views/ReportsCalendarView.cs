using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.Content;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Util;
using MvvmCross.WeakSubscription;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.ViewHolders;
using Toggl.Multivac.Extensions;
using Observable = System.Reactive.Linq.Observable;

namespace Toggl.Giskard.Views
{
    [Register("toggl.giskard.views.ReportsCalendarView")]
    public sealed class ReportsCalendarView : LinearLayout
    {
        private TextView monthYear;
        private LinearLayout daysHeader;
        private ViewPager monthsPager;
        private RecyclerView shortcutsRecyclerView;

        private int rowHeight;
        private int currentRowCount;
        private int? pendingPageUpdate;

        private ReportsCalendarViewModel viewModel;

        private CompositeDisposable disposeBag = new CompositeDisposable();

        public ReportsCalendarView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public ReportsCalendarView(Context context) : base(context)
        {
            Init(Context);
        }

        public ReportsCalendarView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Init(Context);
        }

        public ReportsCalendarView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Init(Context);
        }

        public ReportsCalendarView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
            Init(Context);
        }

        private void Init(Context context)
        {
            Inflate(Context, Resource.Layout.ReportsCalendarView, this);
            SetBackgroundColor(new Android.Graphics.Color(ContextCompat.GetColor(Context, Resource.Color.toolbarBlack)));

            rowHeight = context.Resources.DisplayMetrics.WidthPixels / 7;

            monthYear = FindViewById<TextView>(Resource.Id.ReportsCalendarMonthYear);
            daysHeader = FindViewById<LinearLayout>(Resource.Id.ReportsCalendarFragmentHeader);
            monthsPager = FindViewById<ViewPager>(Resource.Id.ReportsCalendarFragmentViewPager);
            shortcutsRecyclerView = FindViewById<RecyclerView>(Resource.Id.ReportsCalendarFragmentShortcuts);
            shortcutsRecyclerView.SetLayoutManager(new LinearLayoutManager(context, LinearLayoutManager.Horizontal, false));
        }

        public void SetupWith(ReportsCalendarViewModel reportsCalendarViewModel)
        {
            viewModel = reportsCalendarViewModel;

            viewModel.DayHeadersObservable
                .Subscribe(setupWeekdaysLabels)
                .DisposedBy(disposeBag);

            var calendarPagesAdapter = new ReportsCalendarPagerAdapter(Context);
            monthsPager.Adapter = calendarPagesAdapter;

            viewModel.MonthsObservable
                .Subscribe(calendarPagesAdapter.UpdateMonths)
                .DisposedBy(disposeBag);

            monthsPager.Rx().CurrentItem()
                .Subscribe(viewModel.SetCurrentPage)
                .DisposedBy(disposeBag);

            calendarPagesAdapter.DayTaps
                .Subscribe(viewModel.SelectDay.Inputs)
                .DisposedBy(disposeBag);

            viewModel.CurrentPageObservable
                .Subscribe(newPage => monthsPager.SetCurrentItem(newPage, false))
                .DisposedBy(disposeBag);

            viewModel.RowsInCurrentMonthObservable
                .Subscribe(onRowCountChanged)
                .DisposedBy(disposeBag);

            viewModel.SelectedDateRangeObservable
                .CombineLatest(viewModel.QuickSelectShortcutsObservable,
                    viewModel.MonthsObservable,
                    (selectedDateRange, shortcuts, months) => (selectedDateRange, shortcuts, months))
                .Subscribe(tuple => onDateRangeChanged(tuple.Item1, tuple.Item2, tuple.Item3))
                .DisposedBy(disposeBag);

            viewModel.CurrentMonthObservable
                .Select(calendarMonth
                    => $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(calendarMonth.Month)} {calendarMonth.Year}")
                .Subscribe(monthYear.Rx().TextObserver())
                .DisposedBy(disposeBag);

            var shortcutsAdapter = new ReportsCalendarShortcutAdapter();

            shortcutsAdapter.ItemTapObservable
                .Subscribe(viewModel.SelectShortcut.Inputs)
                .DisposedBy(disposeBag);

            shortcutsRecyclerView.SetAdapter(shortcutsAdapter);

            viewModel.QuickSelectShortcutsObservable
                .Subscribe(newShortcuts => shortcutsAdapter.Items = newShortcuts)
                .DisposedBy(disposeBag);

            viewModel.SelectedDateRangeObservable
                .Subscribe(shortcutsAdapter.UpdateSelectedShortcut)
                .DisposedBy(disposeBag);

            viewModel.HighlightedDateRangeObservable
                .Subscribe(calendarPagesAdapter.UpdateSelectedRange)
                .DisposedBy(disposeBag);
        }

        public void ExecutePendingPageUpdate()
        {
            if (pendingPageUpdate.HasValue)
            {
                monthsPager.SetCurrentItem(pendingPageUpdate.Value, true);
                pendingPageUpdate = null;
            }
        }

        private void setupWeekdaysLabels(IReadOnlyList<string> dayHeaders)
        {
            daysHeader
                .GetChildren<TextView>()
                .Indexed()
                .ForEach((textView, index)
                    => textView.Text = dayHeaders[index]);
        }

        private void onDateRangeChanged(ReportsDateRangeParameter dateRange, List<ReportsCalendarBaseQuickSelectShortcut> shortcuts, List<ReportsCalendarPageViewModel> months)
        {
            var anyShortcutIsSelected = shortcuts.Any(shortcut => shortcut.IsSelected(dateRange));
            if (!anyShortcutIsSelected) return;

            var dateRangeStartDate = dateRange.StartDate;
            var monthToScroll = months.IndexOf(month =>
                month.CalendarMonth.Month == dateRangeStartDate.Month
                && month.CalendarMonth.Year == dateRangeStartDate.Year);
            if (monthToScroll == monthsPager.CurrentItem) return;

            var dateRangeStartDateIsContainedInCurrentMonthView = months[monthsPager.CurrentItem].Days
                .Any(day => day.DateTimeOffset == dateRangeStartDate);


            if (!dateRangeStartDateIsContainedInCurrentMonthView || dateRangeStartDate.Month == dateRange.EndDate.Month)
            {
                if (hasScrolledDown())
                {
                    pendingPageUpdate = null;
                    monthsPager.SetCurrentItem(monthToScroll, true);
                }
                else
                {
                    pendingPageUpdate = monthToScroll;
                }
            }
        }

        private bool hasScrolledDown()
        {
            var calendarViewMargins = (MarginLayoutParams) LayoutParameters;
            return calendarViewMargins.TopMargin >= 0;
        }

        private void onRowCountChanged(int rowsInCurrentMonth)
        {
            if (currentRowCount == rowsInCurrentMonth)
                return;

            currentRowCount = rowsInCurrentMonth;
            recalculatePagerHeight();
        }

        private void recalculatePagerHeight()
        {
            var layoutParams = monthsPager.LayoutParameters;
            layoutParams.Height = rowHeight * currentRowCount;
            monthsPager.LayoutParameters = layoutParams;

            var parent = Parent as ReportsLinearLayout;
            parent?.RecalculateCalendarHeight();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing || disposeBag == null) return;

            disposeBag.Dispose();
        }
    }
}
