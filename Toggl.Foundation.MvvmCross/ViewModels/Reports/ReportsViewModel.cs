using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels.Hints;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using Toggl.Foundation.Reports;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models.Reports;
using Toggl.Ultrawave.Exceptions;
using CommonFunctions = Toggl.Multivac.Extensions.CommonFunctions;

[assembly: MvxNavigation(typeof(ReportsViewModel), ApplicationUrls.Reports)]

namespace Toggl.Foundation.MvvmCross.ViewModels.Reports
{
    [Preserve(AllMembers = true)]
    public sealed class ReportsViewModel : MvxViewModel<ReportPeriod>
    {
        private const float minimumSegmentPercentageToBeOnItsOwn = 5f;
        private const float maximumSegmentPercentageToEndUpInOther = 1f;
        private const float minimumOtherSegmentDisplayPercentage = 1f;
        private const float maximumOtherProjectPercentageWithSegmentsBetweenOneAndFivePercent = 5f;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IMvxNavigationService navigationService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IAnalyticsService analyticsService;
        private readonly IDialogService dialogService;
        private readonly IIntentDonationService intentDonationService;
        private readonly IStopwatchProvider stopwatchProvider;

        private readonly ReportsCalendarViewModel calendarViewModel;

        private readonly Subject<Unit> reportSubject = new Subject<Unit>();
        private readonly BehaviorSubject<bool> isLoading = new BehaviorSubject<bool>(true);
        private readonly BehaviorSubject<IThreadSafeWorkspace> workspaceSubject = new BehaviorSubject<IThreadSafeWorkspace>(null);
        private readonly BehaviorSubject<string> currentDateRangeStringSubject = new BehaviorSubject<string>(string.Empty);
        private readonly Subject<DateTimeOffset> startDateSubject = new Subject<DateTimeOffset>();
        private readonly Subject<DateTimeOffset> endDateSubject = new Subject<DateTimeOffset>();
        private readonly ISubject<TimeSpan> totalTimeSubject = new Subject<TimeSpan>();
        private readonly ISubject<float?> billablePercentageSubject = new Subject<float?>();
        private readonly ISubject<IReadOnlyList<ChartSegment>> segmentsSubject = new Subject<IReadOnlyList<ChartSegment>>();

        private bool didNavigateToCalendar;
        private DateTimeOffset startDate;
        private DateTimeOffset endDate;
        private int totalDays => (endDate - startDate).Days + 1;
        private ReportsSource source;

        [Obsolete("This should be removed, replaced by something that is actually used or turned into a constant.")]
        private int projectsNotSyncedCount = 0;

        private DateTime reportSubjectStartTime;
        private long workspaceId;
        private long userId;
        private DateFormat dateFormat;

        public IObservable<bool> IsLoadingObservable { get; }

        public IObservable<TimeSpan> TotalTimeObservable
            => totalTimeSubject.StartWith(TimeSpan.Zero);

        public IObservable<bool> TotalTimeIsZeroObservable
            => TotalTimeObservable.Select(time => time.Ticks == 0);

        public IObservable<DurationFormat> DurationFormatObservable { get; private set; }

        public IObservable<float?> BillablePercentageObservable => billablePercentageSubject.AsObservable();

        public ReportsBarChartViewModel BarChartViewModel { get; }

        public ReportsCalendarViewModel CalendarViewModel => calendarViewModel;

        public IObservable<IReadOnlyList<ChartSegment>> SegmentsObservable { get; private set; }

        public IObservable<IReadOnlyList<ChartSegment>> GroupedSegmentsObservable { get; private set; }

        public IObservable<bool> ShowEmptyStateObservable { get; private set; }

        public IObservable<string> CurrentDateRangeStringObservable { get; }

        public IObservable<string> WorkspaceNameObservable { get; }
        public ICollection<(string ItemName, IThreadSafeWorkspace Item)> Workspaces { get; private set; }
        public IObservable<ICollection<(string ItemName, IThreadSafeWorkspace Item)>> WorkspacesObservable { get; }
        public IObservable<DateTimeOffset> StartDate { get; }
        public IObservable<DateTimeOffset> EndDate { get; }
        public IObservable<bool> WorkspaceHasBillableFeatureEnabled { get; }

        public UIAction SelectWorkspace { get; }

        public ReportsViewModel(ITogglDataSource dataSource,
            ITimeService timeService,
            IMvxNavigationService navigationService,
            IInteractorFactory interactorFactory,
            IAnalyticsService analyticsService,
            IDialogService dialogService,
            IIntentDonationService intentDonationService,
            ISchedulerProvider schedulerProvider,
            IStopwatchProvider stopwatchProvider,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(intentDonationService, nameof(intentDonationService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.timeService = timeService;
            this.navigationService = navigationService;
            this.analyticsService = analyticsService;
            this.dataSource = dataSource;
            this.interactorFactory = interactorFactory;
            this.dialogService = dialogService;
            this.intentDonationService = intentDonationService;
            this.stopwatchProvider = stopwatchProvider;

            calendarViewModel = new ReportsCalendarViewModel(timeService, dialogService, dataSource, intentDonationService, rxActionFactory);

            var totalsObservable = reportSubject
                .SelectMany(_ => dataSource.ReportsProvider.GetTotals(userId, workspaceId, startDate, endDate))
                .Catch<ITimeEntriesTotals, OfflineException>(_ => Observable.Return<ITimeEntriesTotals>(null))
                .Where(report => report != null);
            BarChartViewModel = new ReportsBarChartViewModel(schedulerProvider, dataSource.Preferences, totalsObservable);

            IsLoadingObservable = isLoading.AsObservable().StartWith(true).AsDriver(schedulerProvider);
            StartDate = startDateSubject.AsObservable().AsDriver(schedulerProvider);
            EndDate = endDateSubject.AsObservable().AsDriver(schedulerProvider);

            SelectWorkspace = rxActionFactory.FromAsync(selectWorkspace);

            WorkspaceNameObservable = workspaceSubject
                .Select(workspace => workspace?.Name ?? string.Empty)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            WorkspaceHasBillableFeatureEnabled = workspaceSubject
                .Where(workspace => workspace != null)
                .SelectMany(workspace => dataSource.WorkspaceFeatures.GetById(workspace.Id))
                .Select(workspaceFeatures => workspaceFeatures.IsEnabled(WorkspaceFeatureId.Pro))
                .StartWith(false)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            CurrentDateRangeStringObservable = currentDateRangeStringSubject
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            WorkspacesObservable = interactorFactory.ObserveAllWorkspaces().Execute()
                .Select(list => list.Where(w => !w.IsInaccessible))
                .Select(readOnlyWorkspaceNameTuples)
                .AsDriver(schedulerProvider);

            DurationFormatObservable = dataSource.Preferences.Current
                .Select(prefs => prefs.DurationFormat);

            SegmentsObservable = segmentsSubject.CombineLatest(DurationFormatObservable, applyDurationFormat);
            GroupedSegmentsObservable = SegmentsObservable.CombineLatest(DurationFormatObservable, groupSegments);
            ShowEmptyStateObservable = SegmentsObservable.CombineLatest(IsLoadingObservable, shouldShowEmptyState);
        }

        public override void Prepare(ReportPeriod parameter)
        {
            base.Prepare();
            calendarViewModel.SelectPeriod(parameter);
        }

        public override async Task Initialize()
        {
            WorkspacesObservable
                .Subscribe(data => Workspaces = data)
                .DisposedBy(disposeBag);

            var user = await dataSource.User.Get();
            userId = user.Id;

            var workspace = await interactorFactory.GetDefaultWorkspace()
                .TrackException<InvalidOperationException, IThreadSafeWorkspace>("ReportsViewModel.Initialize")
                .Execute();
            workspaceId = workspace.Id;
            workspaceSubject.OnNext(workspace);

            calendarViewModel.SelectedDateRangeObservable
                .Subscribe(changeDateRange)
                .DisposedBy(disposeBag);

            reportSubject
                .AsObservable()
                .Do(setLoadingState)
                .SelectMany(_ => dataSource.ReportsProvider.GetProjectSummary(workspaceId, startDate, endDate))
                .Subscribe(onReport, onError)
                .DisposedBy(disposeBag);

            dataSource.Preferences.Current
                .Subscribe(onPreferencesChanged)
                .DisposedBy(disposeBag);

            await calendarViewModel.Initialize();
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();

            var firstTimeOpenedFromMainTabBarStopwatch = stopwatchProvider.Get(MeasuredOperation.OpenReportsViewForTheFirstTime);
            stopwatchProvider.Remove(MeasuredOperation.OpenReportsViewForTheFirstTime);
            firstTimeOpenedFromMainTabBarStopwatch?.Stop();
            firstTimeOpenedFromMainTabBarStopwatch = null;

            if (!didNavigateToCalendar)
            {
                navigationService.Navigate(calendarViewModel);
                didNavigateToCalendar = true;
                intentDonationService.DonateShowReport();
                return;
            }

            reportSubject.OnNext(Unit.Default);
        }

        public void StopNavigationFromMainLogStopwatch()
        {
            var navigationStopwatch = stopwatchProvider.Get(MeasuredOperation.OpenReportsFromGiskard);
            stopwatchProvider.Remove(MeasuredOperation.OpenReportsFromGiskard);
            navigationStopwatch?.Stop();
        }

        public void ToggleCalendar()
        {
            navigationService.ChangePresentation(new ToggleReportsCalendarVisibilityHint());
            calendarViewModel.OnToggleCalendar();
        }

        public void HideCalendar()
        {
            navigationService.ChangePresentation(new ToggleReportsCalendarVisibilityHint(forceHide: true));
            calendarViewModel.OnHideCalendar();
        }

        private bool isCurrentWeek()
        {
            var currentDate = timeService.CurrentDateTime.Date;
            var startOfWeek = currentDate.AddDays(1 - (int)currentDate.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(6);

            return startDate.Date == startOfWeek
                   && endDate.Date == endOfWeek;
        }

        private static ReadOnlyCollection<(string, IThreadSafeWorkspace)> readOnlyWorkspaceNameTuples(IEnumerable<IThreadSafeWorkspace> workspaces)
            => workspaces
                .Select(ws => (ws.Name, ws))
                .ToList()
                .AsReadOnly();

        private void setLoadingState(Unit obj)
        {
            reportSubjectStartTime = timeService.CurrentDateTime.UtcDateTime;
            isLoading.OnNext(true);
            segmentsSubject.OnNext(new ChartSegment[0]);
        }

        private void onReport(ProjectSummaryReport report)
        {
            totalTimeSubject.OnNext(TimeSpan.FromSeconds(report.TotalSeconds));
            billablePercentageSubject.OnNext(report.TotalSeconds is 0 ? null : (float?)report.BillablePercentage);
            segmentsSubject.OnNext(report.Segments);
            isLoading.OnNext(false);

            trackReportsEvent(true);
        }

        private void onError(Exception ex)
        {
            isLoading.OnNext(false);
            trackReportsEvent(false);
        }

        private void trackReportsEvent(bool success)
        {
            var loadingTime = timeService.CurrentDateTime.UtcDateTime - reportSubjectStartTime;

            if (success)
            {
                analyticsService.ReportsSuccess.Track(source, totalDays, projectsNotSyncedCount, loadingTime.TotalMilliseconds);
            }
            else
            {
                analyticsService.ReportsFailure.Track(source, totalDays, loadingTime.TotalMilliseconds);
            }
        }

        private void changeDateRange(ReportsDateRangeParameter dateRange)
        {
            startDate = dateRange.StartDate;
            endDate = dateRange.EndDate;
            startDateSubject.OnNext(dateRange.StartDate);
            endDateSubject.OnNext(dateRange.EndDate);
            source = dateRange.Source;
            updateCurrentDateRangeString();
            reportSubject.OnNext(Unit.Default);
        }

        private void updateCurrentDateRangeString()
        {
            if (startDate == default(DateTimeOffset) || endDate == default(DateTimeOffset))
                return;

            if (startDate == endDate)
            {
                currentDateRangeStringSubject.OnNext($"{startDate.ToString(dateFormat.Short)} ▾");
                return;
            }

            currentDateRangeStringSubject.OnNext(isCurrentWeek()
                ? $"{Resources.ThisWeek} ▾"
                : $"{startDate.ToString(dateFormat.Short)} - {endDate.ToString(dateFormat.Short)} ▾");
        }

        private void onPreferencesChanged(IThreadSafePreferences preferences)
        {
            dateFormat = preferences.DateFormat;
            updateCurrentDateRangeString();
        }

        private IReadOnlyList<ChartSegment> applyDurationFormat(IReadOnlyList<ChartSegment> chartSegments, DurationFormat durationFormat)
        {
            return chartSegments.Select(segment => segment.WithDurationFormat(durationFormat))
                .ToList()
                .AsReadOnly();
        }

        private bool shouldShowEmptyState(IReadOnlyList<ChartSegment> chartSegments, bool isLoading)
            => chartSegments.None() && !isLoading;

        private IReadOnlyList<ChartSegment> groupSegments(IReadOnlyList<ChartSegment> segments, DurationFormat durationFormat)
        {
            var groupedData = segments.GroupBy(segment => segment.Percentage >= minimumSegmentPercentageToBeOnItsOwn).ToList();

            var aboveStandAloneThresholdSegments = groupedData
                .Where(group => group.Key)
                .SelectMany(CommonFunctions.Identity)
                .ToList();

            var otherProjectsCandidates = groupedData
                .Where(group => !group.Key)
                .SelectMany(CommonFunctions.Identity)
                .ToList();

            var finalOtherProjects = otherProjectsCandidates
                .Where(segment => segment.Percentage < maximumSegmentPercentageToEndUpInOther)
                .ToList();

            var remainingOtherProjectCandidates = otherProjectsCandidates
                .Except(finalOtherProjects)
                .OrderBy(segment => segment.Percentage)
                .ToList();

            foreach (var segment in remainingOtherProjectCandidates)
            {
                finalOtherProjects.Add(segment);

                if (percentageOf(finalOtherProjects) + segment.Percentage > maximumOtherProjectPercentageWithSegmentsBetweenOneAndFivePercent)
                {
                    break;
                }
            }

            if (!finalOtherProjects.Any())
            {
                return segments;
            }

            var leftOutOfOther = remainingOtherProjectCandidates.Except(finalOtherProjects).ToList();
            aboveStandAloneThresholdSegments.AddRange(leftOutOfOther);
            var onTheirOwnSegments = aboveStandAloneThresholdSegments.OrderBy(segment => segment.Percentage).ToList();

            ChartSegment lastSegment;

            if (finalOtherProjects.Count == 1)
            {
                var singleSmallSegment = finalOtherProjects.First();
                lastSegment = new ChartSegment(
                    singleSmallSegment.ProjectName,
                    string.Empty,
                    singleSmallSegment.Percentage >= minimumOtherSegmentDisplayPercentage ? singleSmallSegment.Percentage : minimumOtherSegmentDisplayPercentage,
                    finalOtherProjects.Sum(segment => (float)segment.TrackedTime.TotalSeconds),
                    finalOtherProjects.Sum(segment => segment.BillableSeconds),
                    singleSmallSegment.Color,
                    durationFormat);
            }
            else
            {
                var otherPercentage = percentageOf(finalOtherProjects);
                lastSegment = new ChartSegment(
                    Resources.Other,
                    string.Empty,
                    otherPercentage >= minimumOtherSegmentDisplayPercentage ? otherPercentage : minimumOtherSegmentDisplayPercentage,
                    finalOtherProjects.Sum(segment => (float)segment.TrackedTime.TotalSeconds),
                    finalOtherProjects.Sum(segment => segment.BillableSeconds),
                    Color.Reports.OtherProjectsSegmentBackground.ToHexString(),
                    durationFormat);
            }

            return onTheirOwnSegments
                .Append(lastSegment)
                .ToList()
                .AsReadOnly();
        }

        private async Task selectWorkspace()
        {
            var currentWorkspaceIndex = Workspaces.IndexOf(w => w.Item.Id == workspaceId);

            var workspace = await dialogService.Select(Resources.SelectWorkspace, Workspaces, currentWorkspaceIndex);

            if (workspace == null || workspace.Id == workspaceId) return;

            workspaceId = workspace.Id;
            workspaceSubject.OnNext(workspace);
            reportSubject.OnNext(Unit.Default);
        }

        private float percentageOf(List<ChartSegment> list)
            => list.Sum(segment => segment.Percentage);
    }
}
