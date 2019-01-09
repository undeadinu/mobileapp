using System;
using System.Collections.Generic;
using System.ComponentModel;
using PropertyChanged;
using Toggl.Foundation.MvvmCross.Interfaces;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar
{
    [Preserve(AllMembers = true)]
    [AddINotifyPropertyChangedInterface]
    public sealed class ReportsCalendarDayViewModel : IDiffable<ReportsCalendarDayViewModel>
    {
        private readonly DateTimeOffset dateTime;

        public int Day { get; }

        public CalendarMonth CalendarMonth { get; }

        public bool IsInCurrentMonth { get; }

        public bool IsToday { get; }

        public DateTimeOffset DateTimeOffset => dateTime;

        public ReportsCalendarDayViewModel(int day, CalendarMonth month, bool isInCurrentMonth, DateTimeOffset today)
        {
            Day = day;
            CalendarMonth = month;
            IsInCurrentMonth = isInCurrentMonth;
            dateTime = new DateTimeOffset(month.Year, month.Month, Day, 0, 0, 0, TimeSpan.Zero);
            IsToday = today.Date == dateTime.Date;
        }

        public bool IsSelected(ReportsDateRangeParameter selectedRange)
        {
            return selectedRange != null && selectedRange.StartDate.Date <= dateTime.Date && selectedRange.EndDate.Date >= dateTime.Date;
        }

        public bool IsStartOfSelectedPeriod(ReportsDateRangeParameter selectedRange)
        {
            return selectedRange != null && selectedRange.StartDate.Date == dateTime.Date;
        }

        public bool IsEndOfSelectedPeriod(ReportsDateRangeParameter selectedRange)
        {
            return selectedRange != null && selectedRange.EndDate.Date == dateTime.Date;
        }

        public bool Equals(ReportsCalendarDayViewModel other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(this, null)) return false;
            if (ReferenceEquals(other, null)) return false;
            return dateTime.Equals(other.dateTime)
                   && Day == other.Day
                   && CalendarMonth.Equals(other.CalendarMonth)
                   && IsInCurrentMonth == other.IsInCurrentMonth
                   && IsToday == other.IsToday;
        }

        public long Identifier => dateTime.Date.GetHashCode();
    }
}
