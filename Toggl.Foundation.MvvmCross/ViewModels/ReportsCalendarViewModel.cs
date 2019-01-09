using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using PropertyChanged;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class ReportsCalendarViewModel : MvxViewModel
    {
        public const int MonthsToShow = 13;

        private readonly string[] dayHeaders =
        {
            Resources.SundayInitial,
            Resources.MondayInitial,
            Resources.TuesdayInitial,
            Resources.WednesdayInitial,
            Resources.ThursdayInitial,
            Resources.FridayInitial,
            Resources.SaturdayInitial
        };
        private readonly ITimeService timeService;
        private readonly IDialogService dialogService;
        private readonly ITogglDataSource dataSource;
        private readonly IIntentDonationService intentDonationService;
        private readonly ISubject<ReportsDateRangeParameter> selectedDateRangeSubject = new Subject<ReportsDateRangeParameter>();
        private readonly ISubject<ReportsDateRangeParameter> highlightedDateRangeSubject = new BehaviorSubject<ReportsDateRangeParameter>(default(ReportsDateRangeParameter));
        private IObservable<BeginningOfWeek> beginningOfWeekObservable;

        private bool isInitialized;
        private CalendarMonth initialMonth;
        private ReportsCalendarDayViewModel startOfSelection;
        private ReportPeriod reportPeriod = ReportPeriod.ThisWeek;
        private BeginningOfWeek beginningOfWeek;

        public IObservable<int> RowsInCurrentMonthObservable { get; private set; }

        public IObservable<CalendarMonth> CurrentMonthObservable { get; private set; }

        private readonly ISubject<int> currentPageSubject = new Subject<int>();

        public IObservable<int> CurrentPageObservable { get; }

        public IObservable<ReportsDateRangeParameter> SelectedDateRangeObservable
            => selectedDateRangeSubject.AsObservable();

        public IObservable<ReportsDateRangeParameter> HighlightedDateRangeObservable
            => highlightedDateRangeSubject.AsObservable();

        public List<ReportsCalendarBaseQuickSelectShortcut> QuickSelectShortcuts { get; private set; }

        public IObservable<List<ReportsCalendarBaseQuickSelectShortcut>> QuickSelectShortcutsObservable { get; private set; }

        public IObservable<List<ReportsCalendarPageViewModel>> MonthsObservable { get; private set; }

        public IObservable<IReadOnlyList<string>> DayHeadersObservable { get; private set; }

        public readonly InputAction<ReportsCalendarDayViewModel> SelectDay;

        public readonly InputAction<ReportsCalendarBaseQuickSelectShortcut> SelectShortcut;

        public ReportsCalendarViewModel(
            ITimeService timeService,
            IDialogService dialogService,
            ITogglDataSource dataSource,
            IIntentDonationService intentDonationService,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(intentDonationService, nameof(intentDonationService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.timeService = timeService;
            this.dialogService = dialogService;
            this.dataSource = dataSource;
            this.intentDonationService = intentDonationService;

            SelectDay = rxActionFactory.FromAsync<ReportsCalendarDayViewModel>(calendarDayTapped);
            SelectShortcut = rxActionFactory.FromAction<ReportsCalendarBaseQuickSelectShortcut>(quickSelect);

            CurrentPageObservable = currentPageSubject
                .StartWith(MonthsToShow - 1)
                .DistinctUntilChanged();
        }

        public void SetCurrentPage(int newPage)
        {
            currentPageSubject.OnNext(newPage);
        }

        public override void Prepare()
        {
            base.Prepare();

            var now = timeService.CurrentDateTime;
            initialMonth = new CalendarMonth(now.Year, now.Month).AddMonths(-(MonthsToShow - 1));
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            beginningOfWeekObservable = dataSource.User.Current
                .Select(user => user.BeginningOfWeek)
                .DistinctUntilChanged();

            DayHeadersObservable = beginningOfWeekObservable.Select(mapDayHeaders);

            MonthsObservable = beginningOfWeekObservable.CombineLatest(
                timeService.MidnightObservable.StartWith(timeService.CurrentDateTime),
                (beginningOfWeek, today) =>
                {
                    var monthIterator = new CalendarMonth(today.Year, today.Month).AddMonths(-(MonthsToShow - 1));
                    var months = new List<ReportsCalendarPageViewModel>();
                    for (int i = 0; i < MonthsToShow; i++, monthIterator = monthIterator.Next())
                        months.Add(new ReportsCalendarPageViewModel(monthIterator, beginningOfWeek, today));
                    return months;
                });

            RowsInCurrentMonthObservable = MonthsObservable.CombineLatest(
                CurrentPageObservable,
                (months, page) => months[page].RowCount)
                .Select(CommonFunctions.Identity);

            CurrentMonthObservable = CurrentPageObservable.Select(convertPageIndexToCalendarMonth);

            QuickSelectShortcutsObservable = beginningOfWeekObservable.Select(createQuickSelectShortcuts);

            beginningOfWeek = (await dataSource.User.Current.FirstAsync()).BeginningOfWeek;

            QuickSelectShortcuts = createQuickSelectShortcuts(beginningOfWeek);

            SelectPeriod(reportPeriod);

            isInitialized = true;
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();
            var initialShortcut = QuickSelectShortcuts.Single(shortcut => shortcut.Period == reportPeriod);
            selectedDateRangeSubject.OnNext(initialShortcut.GetDateRange().WithSource(ReportsSource.Initial));
            highlightedDateRangeSubject.OnNext(initialShortcut.GetDateRange().WithSource(ReportsSource.Initial));
        }

        public void OnToggleCalendar() => selectStartOfSelectionIfNeeded();

        public void OnHideCalendar() => selectStartOfSelectionIfNeeded();

        public void SelectPeriod(ReportPeriod period)
        {
            reportPeriod = period;

            if (isInitialized)
            {
                var initialShortcut = QuickSelectShortcuts.Single(shortcut => shortcut.Period == period);
                changeDateRange(initialShortcut.GetDateRange().WithSource(ReportsSource.Initial));
            }
        }

        private async Task calendarDayTapped(ReportsCalendarDayViewModel tappedDay)
        {
            if (startOfSelection == null)
            {
                var date = tappedDay.DateTimeOffset;

                var dateRange = ReportsDateRangeParameter
                    .WithDates(date, date)
                    .WithSource(ReportsSource.Calendar);
                startOfSelection = tappedDay;
                highlightDateRange(dateRange);
            }
            else
            {
                var startDate = startOfSelection.DateTimeOffset;
                var endDate = tappedDay.DateTimeOffset;

                if (System.Math.Abs((endDate - startDate).Days) > 365)
                {
                    await dialogService.Alert(
                        Resources.ReportTooLongTitle,
                        Resources.ReportTooLongDescription,
                        Resources.Ok
                    );
                }
                else
                {
                    var dateRange = ReportsDateRangeParameter
                        .WithDates(startDate, endDate)
                        .WithSource(ReportsSource.Calendar);
                    startOfSelection = null;
                    changeDateRange(dateRange);
                }
            }
        }

        private IReadOnlyList<string> mapDayHeaders(BeginningOfWeek newBeginningOfWeek)
        {
            var updatedDayHeaders = new List<string>();
            for (var i = 0; i < dayHeaders.Length; i++)
            {
                updatedDayHeaders.Add(dayHeaderFor(i, newBeginningOfWeek));
            }

            return updatedDayHeaders.AsReadOnly();
        }

        private string dayHeaderFor(int index, BeginningOfWeek newBeginningOfWeek)
            => dayHeaders[(index + (int)newBeginningOfWeek + 7) % 7];

        private void selectStartOfSelectionIfNeeded()
        {
            if (startOfSelection == null) return;

            var date = startOfSelection.DateTimeOffset;
            var dateRange = ReportsDateRangeParameter
                .WithDates(date, date)
                .WithSource(ReportsSource.Calendar);
            changeDateRange(dateRange);
        }

        private List<ReportsCalendarBaseQuickSelectShortcut> createQuickSelectShortcuts(BeginningOfWeek beginningOfWeek)
            => new List<ReportsCalendarBaseQuickSelectShortcut>
            {
                new ReportsCalendarTodayQuickSelectShortcut(timeService),
                new ReportsCalendarYesterdayQuickSelectShortcut(timeService),
                new ReportsCalendarThisWeekQuickSelectShortcut(timeService, beginningOfWeek),
                new ReportsCalendarLastWeekQuickSelectShortcut(timeService, beginningOfWeek),
                new ReportsCalendarThisMonthQuickSelectShortcut(timeService),
                new ReportsCalendarLastMonthQuickSelectShortcut(timeService),
                new ReportsCalendarThisYearQuickSelectShortcut(timeService)
            };

        private CalendarMonth convertPageIndexToCalendarMonth(int pageIndex)
            => initialMonth.AddMonths(pageIndex);

        private void changeDateRange(ReportsDateRangeParameter newDateRange)
        {
            startOfSelection = null;

            highlightDateRange(newDateRange);

            selectedDateRangeSubject.OnNext(newDateRange);
        }

        private void quickSelect(ReportsCalendarBaseQuickSelectShortcut quickSelectShortCut)
        {
            intentDonationService.DonateShowReport(quickSelectShortCut.Period);
            changeDateRange(quickSelectShortCut.GetDateRange());
        }

        private void highlightDateRange(ReportsDateRangeParameter dateRange)
        {
            highlightedDateRangeSubject.OnNext(dateRange);
        }
    }
}
