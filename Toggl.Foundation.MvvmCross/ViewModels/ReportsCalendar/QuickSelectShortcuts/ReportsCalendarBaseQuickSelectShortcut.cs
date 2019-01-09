using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.Interfaces;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;
using Toggl.Foundation.Services;

namespace Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts
{
    [Preserve(AllMembers = true)]
    public abstract class ReportsCalendarBaseQuickSelectShortcut : IDiffable<ReportsCalendarBaseQuickSelectShortcut>
    {
        protected ITimeService TimeService { get; private set; }

        public string Title { get; }

        public ReportPeriod Period { get; private set; }

        protected ReportsCalendarBaseQuickSelectShortcut(
            ITimeService timeService, string title, ReportPeriod reportPeriod)
        {
            Ensure.Argument.IsNotNull(title, nameof(title));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(reportPeriod, nameof(reportPeriod));

            Title = title;
            Period = reportPeriod;
            TimeService = timeService;
        }

        public bool IsSelected(ReportsDateRangeParameter dateRange)
        {
            var thisActionDateRange = GetDateRange();

            return dateRange != null
                   && dateRange.StartDate.Date == thisActionDateRange.StartDate.Date
                    && dateRange.EndDate.Date == thisActionDateRange.EndDate.Date;
        }

        public abstract ReportsDateRangeParameter GetDateRange();
        public bool Equals(ReportsCalendarBaseQuickSelectShortcut other)
        {
            if (other == null) return false;
            return Title.Equals(other.Title);
        }

        public long Identifier => (long)Period;
    }
}
