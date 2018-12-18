using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

[assembly: MvxNavigation(typeof(CalendarViewModel), ApplicationUrls.Calendar.Regex)]
namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarViewModel : MvxViewModel
    {
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IDialogService dialogService;
        private readonly IUserPreferences userPreferences;
        private readonly IAnalyticsService analyticsService;
        private readonly IBackgroundService backgroundService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IPermissionsService permissionsService;
        private readonly IMvxNavigationService navigationService;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly IRxActionFactory rxActionFactory;

        private readonly ISubject<bool> shouldShowOnboardingSubject;
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        public IObservable<bool> SettingsAreVisible { get; }

        public IObservable<bool> ShouldShowOnboarding { get; }

        public IObservable<TimeFormat> TimeOfDayFormat { get; }

        public UIAction GetStarted { get; }

        public UIAction SelectCalendars { get; }

        public InputAction<CalendarItem> OnItemTapped { get; }

        public InputAction<(DateTimeOffset, TimeSpan)> OnDurationSelected { get; }

        public InputAction<CalendarItem> OnUpdateTimeEntry { get; }

        public ObservableGroupedOrderedCollection<CalendarItem> CalendarItems { get; }

        public CalendarViewModel(
            ITogglDataSource dataSource,
            ITimeService timeService,
            IDialogService dialogService,
            IUserPreferences userPreferences,
            IAnalyticsService analyticsService,
            IBackgroundService backgroundService,
            IInteractorFactory interactorFactory,
            IOnboardingStorage onboardingStorage,
            ISchedulerProvider schedulerProvider,
            IPermissionsService permissionsService,
            IMvxNavigationService navigationService,
            IStopwatchProvider stopwatchProvider,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(backgroundService, nameof(backgroundService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.dialogService = dialogService;
            this.userPreferences = userPreferences;
            this.analyticsService = analyticsService;
            this.backgroundService = backgroundService;
            this.interactorFactory = interactorFactory;
            this.onboardingStorage = onboardingStorage;
            this.navigationService = navigationService;
            this.permissionsService = permissionsService;
            this.stopwatchProvider = stopwatchProvider;
            this.rxActionFactory = rxActionFactory;

            var isCompleted = onboardingStorage.CompletedCalendarOnboarding();
            shouldShowOnboardingSubject = new BehaviorSubject<bool>(!isCompleted);

            var onboardingObservable = shouldShowOnboardingSubject
                .AsObservable()
                .DistinctUntilChanged();

            ShouldShowOnboarding = onboardingObservable.AsDriver(false, schedulerProvider);

            TimeOfDayFormat = dataSource
                .Preferences
                .Current
                .Select(preferences => preferences.TimeOfDayFormat);

            GetStarted = rxActionFactory.FromAsync(getStarted);
            OnItemTapped = rxActionFactory.FromAsync<CalendarItem>(handleCalendarItem);

            SettingsAreVisible = onboardingObservable
                .SelectMany(_ => permissionsService.CalendarPermissionGranted)
                .DistinctUntilChanged();

            SelectCalendars = rxActionFactory.FromAsync(() => selectUserCalendars(false), SettingsAreVisible);

            OnDurationSelected = rxActionFactory.FromAsync<(DateTimeOffset StartTime, TimeSpan Duration)>(
                tuple => durationSelected(tuple.StartTime, tuple.Duration));

            OnUpdateTimeEntry = rxActionFactory.FromAsync<CalendarItem>(updateTimeEntry);

            CalendarItems = new ObservableGroupedOrderedCollection<CalendarItem>(
                indexKey: item => item.StartTime,
                orderingKey: item => item.StartTime,
                groupingKey: _ => 0);
        }

        public void Init(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                return;

            interactorFactory
                .GetCalendarItemWithId(eventId)
                .Execute()
                .SelectMany(calendarItem =>
                    handleCalendarItem(calendarItem).ToObservable())
                .Subscribe()
                .DisposedBy(disposeBag);
        }

        public async override Task Initialize()
        {
            var dayChangedObservable = timeService
                .MidnightObservable
                .SelectUnit();

            var selectedCalendarsChangedObservable = userPreferences
                .EnabledCalendars
                .SelectUnit();

            var appResumedFromBackgroundObservable = backgroundService
                .AppResumedFromBackground
                .SelectUnit();

            dataSource.TimeEntries
                .ItemsChanged()
                .Merge(dayChangedObservable)
                .Merge(selectedCalendarsChangedObservable)
                .Merge(appResumedFromBackgroundObservable)
                .SelectMany(_ => reloadData())
                .Subscribe(CalendarItems.ReplaceWith)
                .DisposedBy(disposeBag);

            selectedCalendarsChangedObservable
                .Merge(dayChangedObservable)
                .Merge(appResumedFromBackgroundObservable)
                .StartWith(Unit.Default)
                .CombineLatest(userPreferences.CalendarNotificationsEnabled, CommonFunctions.Second)
                .SelectMany(refreshNotifications)
                .Subscribe()
                .DisposedBy(disposeBag);
        }

        private IObservable<Unit> refreshNotifications(bool notificationsAreEnabled)
            => Observable.FromAsync(async () =>
            {
                await interactorFactory.UnscheduleAllNotifications().Execute();

                if (!notificationsAreEnabled)
                    return;

                await interactorFactory.ScheduleEventNotificationsForNextWeek().Execute();
            });

        private async Task getStarted()
        {
            analyticsService.CalendarOnboardingStarted.Track();
            var calendarPermissionGranted = await permissionsService.RequestCalendarAuthorization();
            if (calendarPermissionGranted)
            {
                await selectUserCalendars(true);
                var notificationPermissionGranted = await permissionsService.RequestNotificationAuthorization();
                userPreferences.SetCalendarNotificationsEnabled(notificationPermissionGranted);
            }
            else
            {
                await navigationService.Navigate<CalendarPermissionDeniedViewModel, Unit>();
            }

            onboardingStorage.SetCompletedCalendarOnboarding();
            shouldShowOnboardingSubject.OnNext(false);
        }

        private async Task selectUserCalendars(bool isOnboarding)
        {
            var calendarsExist = await interactorFactory
                .GetUserCalendars()
                .Execute()
                .Select(calendars => calendars.Any());

            if (calendarsExist)
            {
                var calendarIds = await navigationService
                    .Navigate<SelectUserCalendarsViewModel, bool, string[]>(isOnboarding);

                interactorFactory.SetEnabledCalendars(calendarIds).Execute();
            }
            else if (!isOnboarding)
            {
                await dialogService.Alert(Resources.Oops, Resources.NoCalendarsFoundMessage, Resources.Ok);
            }
        }

        private async Task handleCalendarItem(CalendarItem calendarItem)
        {
            switch (calendarItem.Source)
            {
                case CalendarItemSource.TimeEntry when calendarItem.TimeEntryId.HasValue:
                    analyticsService.EditViewOpenedFromCalendar.Track();
                    var stopwatch = stopwatchProvider.CreateAndStore(MeasuredOperation.EditTimeEntryFromCalendar);
                    stopwatch.Start();
                    await navigationService.Navigate<EditTimeEntryViewModel, long>(calendarItem.TimeEntryId.Value);
                    break;

                case CalendarItemSource.Calendar:
                    var workspace = await interactorFactory.GetDefaultWorkspace()
                        .TrackException<InvalidOperationException, IThreadSafeWorkspace>("CalendarViewModel.handleCalendarItem")
                        .Execute();
                    var prototype = calendarItem.AsTimeEntryPrototype(workspace.Id);
                    analyticsService.TimeEntryStarted.Track(TimeEntryStartOrigin.CalendarEvent);
                    await interactorFactory.CreateTimeEntry(prototype).Execute();
                    break;
            }
        }

        private async Task durationSelected(DateTimeOffset startTime, TimeSpan duration)
        {
            var workspace = await interactorFactory.GetDefaultWorkspace()
                .TrackException<InvalidOperationException, IThreadSafeWorkspace>("CalendarViewModel.durationSelected")
                .Execute();

            var prototype = duration.AsTimeEntryPrototype(startTime, workspace.Id);
            var timeEntry = await interactorFactory.CreateTimeEntry(prototype).Execute();
            analyticsService.TimeEntryStarted.Track(TimeEntryStartOrigin.CalendarTapAndDrag);
            await navigationService.Navigate<EditTimeEntryViewModel, long>(timeEntry.Id);
        }

        private async Task updateTimeEntry(CalendarItem calendarItem)
        {
            if (!calendarItem.IsEditable() || calendarItem.TimeEntryId == null)
                return;

            var timeEntry = await dataSource.TimeEntries.GetById(calendarItem.TimeEntryId.Value);

            var dto = new DTOs.EditTimeEntryDto
            {
                Id = timeEntry.Id,
                Description = timeEntry.Description,
                StartTime = calendarItem.StartTime,
                StopTime = calendarItem.EndTime,
                ProjectId = timeEntry.ProjectId,
                TaskId = timeEntry.TaskId,
                Billable = timeEntry.Billable,
                WorkspaceId = timeEntry.WorkspaceId,
                TagIds = timeEntry.TagIds
            };

            var duration = calendarItem.Duration.HasValue 
                ? calendarItem.Duration.Value.TotalSeconds 
                : (timeService.CurrentDateTime - calendarItem.StartTime.LocalDateTime).TotalSeconds;

            if (timeEntry.Duration != duration)
            {
                analyticsService.TimeEntryChangedFromCalendar.Track(CalendarChangeEvent.Duration);
            }

            if (timeEntry.Start != calendarItem.StartTime)
            {
                analyticsService.TimeEntryChangedFromCalendar.Track(CalendarChangeEvent.StartTime);
            }

            await interactorFactory.UpdateTimeEntry(dto).Execute();
        }

        private IObservable<IEnumerable<CalendarItem>> reloadData()
        {
            return interactorFactory.GetCalendarItemsForDate(timeService.CurrentDateTime.ToLocalTime().Date).Execute();
        }
    }
}
