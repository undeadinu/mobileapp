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

        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IMvxNavigationService navigationService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IAnalyticsService analyticsService;
        private readonly IDialogService dialogService;
        private readonly IIntentDonationService intentDonationService;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly IRxActionFactory rxActionFactory;

        private readonly ReportsCalendarViewModel calendarViewModel;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        private readonly Subject<Unit> reportSubject = new Subject<Unit>();
        private readonly BehaviorSubject<bool> isLoading = new BehaviorSubject<bool>(true);
        private readonly BehaviorSubject<IThreadSafeWorkspace> workspaceSubject = new BehaviorSubject<IThreadSafeWorkspace>(null);
        private readonly BehaviorSubject<string> currentDateRangeStringSubject = new BehaviorSubject<string>(string.Empty);
        private readonly Subject<DateTimeOffset> startDateSubject = new Subject<DateTimeOffset>();
        private readonly Subject<DateTimeOffset> endDateSubject = new Subject<DateTimeOffset>();

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
        private IReadOnlyList<ChartSegment> segments = new ChartSegment[0];
        private IReadOnlyList<ChartSegment> groupedSegments = new ChartSegment[0];

        [Obsolete("Use IsLoadingObservable instead")]
        public bool IsLoading { get; private set; }
        public IObservable<bool> IsLoadingObservable { get; }

        public TimeSpan TotalTime { get; private set; } = TimeSpan.Zero;

        public DurationFormat DurationFormat { get; private set; }

        public bool TotalTimeIsZero => TotalTime.Ticks == 0;

        public float? BillablePercentage { get; private set; }

        public ReportsBarChartViewModel BarChartViewModel { get; }

        public IReadOnlyList<ChartSegment> Segments
        {
            get => segments;
            private set
            {
                segments = value;
                groupedSegments = null;
            }
        }

        [DependsOn(nameof(Segments))]
        public IReadOnlyList<ChartSegment> GroupedSegments
            => groupedSegments ?? (groupedSegments = groupSegments());

        public bool ShowEmptyState => segments.None() && !IsLoading;

        [Obsolete("Use CurrentDateRangeStringObservable instead")]
        public string CurrentDateRangeString { get; private set; }
        public IObservable<string> CurrentDateRangeStringObservable { get; }

        public bool IsCurrentWeek
        {
            get
            {
                var currentDate = timeService.CurrentDateTime.Date;
                var startOfWeek = currentDate.AddDays(1 - (int)currentDate.DayOfWeek);
                var endOfWeek = startOfWeek.AddDays(6);

                return startDate.Date == startOfWeek
                       && endDate.Date == endOfWeek;
            }
        }

        [Obsolete("Use WorkspaceNameObservable instead")]
        public string WorkspaceName { get; private set; }
        public IObservable<string> WorkspaceNameObservable { get; }

        [Obsolete("Use WorkspacesObservable instead")]
        public ICollection<(string ItemName, IThreadSafeWorkspace Item)> Workspaces { get; private set; }
        public IObservable<ICollection<(string ItemName, IThreadSafeWorkspace Item)>> WorkspacesObservable { get; }

        [Obsolete("Use HideCalendar instead")]
        public IMvxCommand HideCalendarCommand { get; }
        [Obsolete("Use ToggleCalendar instead")]
        public IMvxCommand ToggleCalendarCommand { get; }
        [Obsolete]
        public IMvxCommand<ReportsDateRangeParameter> ChangeDateRangeCommand { get; }

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
            this.rxActionFactory = rxActionFactory;

            calendarViewModel = new ReportsCalendarViewModel(timeService, dialogService, dataSource, intentDonationService);

            var totalsObservable = reportSubject
                .SelectMany(_ => dataSource.ReportsProvider.GetTotals(userId, workspaceId, startDate, endDate))
                .Catch<ITimeEntriesTotals, OfflineException>(_ => Observable.Return<ITimeEntriesTotals>(null))
                .Where(report => report != null);
            BarChartViewModel = new ReportsBarChartViewModel(schedulerProvider, dataSource.Preferences, totalsObservable);

            HideCalendarCommand = new MvxCommand(HideCalendar);
            ToggleCalendarCommand = new MvxCommand(ToggleCalendar);
            ChangeDateRangeCommand = new MvxCommand<ReportsDateRangeParameter>(changeDateRange);

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
        }

        public override void Prepare(ReportPeriod parameter)
        {
            base.Prepare();
            calendarViewModel.SelectPeriod(parameter);
        }

        public override async Task Initialize()
        {
            Workspaces = await interactorFactory.GetAllWorkspaces().Execute()
                .Select(readOnlyWorkspaceNameTuples);

            var user = await dataSource.User.Get();
            userId = user.Id;

            var workspace = await interactorFactory.GetDefaultWorkspace()
                .TrackException<InvalidOperationException, IThreadSafeWorkspace>("ReportsViewModel.Initialize")
                .Execute();
            workspaceId = workspace.Id;
            workspaceSubject.OnNext(workspace);

            reportSubject
                .AsObservable()
                .Do(setLoadingState)
                .SelectMany(_ => dataSource.ReportsProvider.GetProjectSummary(workspaceId, startDate, endDate))
                .Subscribe(onReport, onError)
                .DisposedBy(disposeBag);

            calendarViewModel.SelectedDateRangeObservable
                .Subscribe(changeDateRange)
                .DisposedBy(disposeBag);

            dataSource.Preferences.Current
                .Subscribe(onPreferencesChanged)
                .DisposedBy(disposeBag);

            //Depricated properties still used in android
            CurrentDateRangeStringObservable
                .Subscribe(range => CurrentDateRangeString = range)
                .DisposedBy(disposeBag);

            WorkspaceNameObservable
                .Subscribe(name => WorkspaceName = name)
                .DisposedBy(disposeBag);

            WorkspacesObservable
                .Subscribe(data => Workspaces = data)
                .DisposedBy(disposeBag);

            IsLoading = true;
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

        private static ReadOnlyCollection<(string, IThreadSafeWorkspace)> readOnlyWorkspaceNameTuples(IEnumerable<IThreadSafeWorkspace> workspaces)
            => workspaces
                .Select(ws => (ws.Name, ws))
                .ToList()
                .AsReadOnly();

        private void setLoadingState(Unit obj)
        {
            reportSubjectStartTime = timeService.CurrentDateTime.UtcDateTime;
            IsLoading = true;
            isLoading.OnNext(true);
            Segments = new ChartSegment[0];
        }

        private void onReport(ProjectSummaryReport report)
        {
            TotalTime = TimeSpan.FromSeconds(report.TotalSeconds);
            BillablePercentage = report.TotalSeconds is 0 ? null : (float?)report.BillablePercentage;

            Segments = report.Segments
                             .Select(segment => segment.WithDurationFormat(DurationFormat))
                             .ToList()
                             .AsReadOnly();

            IsLoading = false;
            isLoading.OnNext(false);

            trackReportsEvent(true);
        }

        private void onError(Exception ex)
        {
            RaisePropertyChanged(nameof(Segments));
            IsLoading = false;
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

            currentDateRangeStringSubject.OnNext(IsCurrentWeek
                ? $"{Resources.ThisWeek} ▾"
                : $"{startDate.ToString(dateFormat.Short)} - {endDate.ToString(dateFormat.Short)} ▾");
        }

        private void onPreferencesChanged(IThreadSafePreferences preferences)
        {
            DurationFormat = preferences.DurationFormat;
            dateFormat = preferences.DateFormat;

            Segments = segments.Select(segment => segment.WithDurationFormat(DurationFormat))
                               .ToList()
                               .AsReadOnly();

            updateCurrentDateRangeString();
        }

        private IReadOnlyList<ChartSegment> groupSegments()
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
                    DurationFormat);
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
                    DurationFormat);
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
