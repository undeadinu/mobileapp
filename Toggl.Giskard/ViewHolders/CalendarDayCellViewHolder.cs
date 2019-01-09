using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using MvvmCross.Plugin.Color.Platforms.Android;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar;
using Toggl.Giskard.Views;
using static Toggl.Foundation.MvvmCross.Helper.Color.Reports;

namespace Toggl.Giskard.ViewHolders
{
    public sealed class CalendarDayCellViewHolder : BaseRecyclerViewHolder<ReportsCalendarDayViewModel>
    {
        private ReportsCalendarDayView dayView;

        public CalendarDayCellViewHolder(Context context) : base(new ReportsCalendarDayView(context) { Gravity = GravityFlags.Center })
        {
        }

        public CalendarDayCellViewHolder(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            dayView = ItemView as ReportsCalendarDayView;
        }

        protected override void UpdateView()
        {
            dayView.Text = Item.Day.ToString();
            dayView.IsToday = Item.IsToday;
        }

        public void UpdateSelectionState(ReportsDateRangeParameter selectedDateRange)
        {
            dayView.SetTextColor(Item.IsSelected(selectedDateRange) ? Color.White : DayNotInMonth.ToNativeColor());
            dayView.RoundLeft = Item.IsStartOfSelectedPeriod(selectedDateRange);
            dayView.RoundRight = Item.IsEndOfSelectedPeriod(selectedDateRange);
            dayView.IsSelected = Item.IsSelected(selectedDateRange);
        }
    }
}
