using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Helper.Constants;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class EditDurationViewModel : MvxViewModel<EditDurationParameters, DurationParameter>
    {
        private readonly ITimeService timeService;
        private readonly IMvxNavigationService navigationService;
        private readonly ITogglDataSource dataSource;
        private readonly IAnalyticsService analyticsService;
        private readonly IRxActionFactory rxActionFactory;

        private IDisposable runningTimeEntryDisposable;
        private IDisposable preferencesDisposable;

        private DurationParameter defaultResult;

        private DurationFormat durationFormat;

        private EditDurationEvent analyticsEvent;

        [DependsOn(nameof(IsRunning))]
        public DurationFormat DurationFormat => IsRunning ? DurationFormat.Improved : durationFormat;

        public DateFormat DateFormat { get; private set; }

        public TimeFormat TimeFormat { get; private set; }

        public bool IsRunning { get; private set; }

        public DateTimeOffset StartTime { get; private set; }

        public DateTimeOffset StopTime { get; private set; }

        public bool IsDurationInitiallyFocused { get; private set; }

        [DependsOn(nameof(StartTime), nameof(StopTime))]
        public TimeSpan Duration
        {
            get => StopTime - StartTime;
            set
            {
                if (Duration == value) return;

                onDurationChanged(value);
            }
        }

        public DateTimeOffset EditedTime
        {
            get
            {
                switch (editMode.Value)
                {
                    case EditMode.StartTime:
                        return StartTime;

                    case EditMode.EndTime:
                        return StopTime;

                    default:
                        // any value between start and end time can be returned here
                        // this constraint is to avoid invalid dates with the date picker
                        return StartTime;
                }
            }

            set
            {
                if (editMode.Value == EditMode.None) return;

                var valueInRange = value.Clamp(MinimumDateTime, MaximumDateTime);

                switch (editMode.Value)
                {
                    case EditMode.StartTime:
                        StartTime = valueInRange;
                        break;

                    case EditMode.EndTime:
                        StopTime = valueInRange;
                        break;
                }
            }
        }

        private Subject<Unit> startTimeChangingSubject = new Subject<Unit>();
        public IObservable<Unit> StartTimeChanging
            => startTimeChangingSubject.AsObservable();

        public DateTime MinimumDateTime { get; private set; }

        public DateTime MaximumDateTime { get; private set; }

        public DateTimeOffset MinimumStartTime => StopTime.AddHours(-MaxTimeEntryDurationInHours);

        public DateTimeOffset MaximumStartTime => StopTime;

        public DateTimeOffset MinimumStopTime => StartTime;

        public DateTimeOffset MaximumStopTime => StartTime.AddHours(MaxTimeEntryDurationInHours);






        private BehaviorSubject<EditMode> editMode = new BehaviorSubject<EditMode>(EditMode.None);

        public UIAction Save { get; }
        public UIAction Close { get; }
        public UIAction EditStartTime { get; }
        public UIAction EditStopTime { get; }
        public UIAction StopEditingTime { get; }

        public IObservable<bool> IsEditingTime { get; }
        public IObservable<bool> IsEditingStartTime { get; }
        public IObservable<bool> IsEditingStopTime { get; }


        public EditDurationViewModel(IMvxNavigationService navigationService, ITimeService timeService, ITogglDataSource dataSource, IAnalyticsService analyticsService, IRxActionFactory rxActionFactory, ISchedulerProvider schedulerProvider)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.timeService = timeService;
            this.navigationService = navigationService;
            this.dataSource = dataSource;
            this.analyticsService = analyticsService;
            this.rxActionFactory = rxActionFactory;

            Save = rxActionFactory.FromAsync(save);
            Close = rxActionFactory.FromAsync(close);
            EditStartTime = rxActionFactory.FromAction(editStartTime);
            EditStopTime = rxActionFactory.FromAction(editStopTime);
            StopEditingTime = rxActionFactory.FromAction(stopEditingTime);

            IsEditingTime = editMode.Select(v => v != EditMode.None).AsDriver(schedulerProvider);
            IsEditingStartTime = editMode.Select(v => v == EditMode.StartTime).AsDriver(schedulerProvider);
            IsEditingStopTime = editMode.Select(v => v == EditMode.EndTime).AsDriver(schedulerProvider);
        }

        public override void Prepare(EditDurationParameters parameter)
        {
            defaultResult = parameter.DurationParam;
            IsRunning = defaultResult.Duration.HasValue == false;

            analyticsEvent = new EditDurationEvent(IsRunning,
                parameter.IsStartingNewEntry
                    ? EditDurationEvent.NavigationOrigin.Start
                    : EditDurationEvent.NavigationOrigin.Edit);

            if (IsRunning)
            {
                runningTimeEntryDisposable = timeService.CurrentDateTimeObservable
                           .Subscribe(currentTime => StopTime = currentTime);
            }

            StartTime = parameter.DurationParam.Start;
            StopTime = parameter.DurationParam.Duration.HasValue
                ? StartTime + parameter.DurationParam.Duration.Value
                : timeService.CurrentDateTime;

            MinimumDateTime = StartTime.DateTime;
            MaximumDateTime = StopTime.DateTime;
            IsDurationInitiallyFocused = parameter.IsDurationInitiallyFocused;
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            preferencesDisposable = dataSource.Preferences.Current
                .Subscribe(onPreferencesChanged);

            editMode.OnNext(EditMode.None);
        }

        public void TimeEditedWithSource(EditTimeSource source)
        {
            analyticsEvent = analyticsEvent.UpdateWith(source);
        }

        private Task close()
        {
            analyticsEvent = analyticsEvent.With(result: EditDurationEvent.Result.Cancel);
            analyticsService.Track(analyticsEvent);
            return navigationService.Close(this, defaultResult);
        }

        private Task save()
        {
            analyticsEvent = analyticsEvent.With(result: EditDurationEvent.Result.Save);
            analyticsService.Track(analyticsEvent);
            var result = DurationParameter.WithStartAndDuration(StartTime, IsRunning ? (TimeSpan?)null : Duration);
            return navigationService.Close(this, result);
        }

        private void editStartTime()
        {
            if (editMode.Value == EditMode.StartTime)
            {
                editMode.OnNext(EditMode.None);
            }
            else
            {
                startTimeChangingSubject.OnNext(Unit.Default);
                MinimumDateTime = MinimumStartTime.LocalDateTime;
                MaximumDateTime = MaximumStartTime.LocalDateTime;

                editMode.OnNext(EditMode.StartTime);
            }

            RaisePropertyChanged(nameof(EditedTime));
        }

        private void editStopTime()
        {
            if (IsRunning)
            {
                runningTimeEntryDisposable?.Dispose();
                StopTime = timeService.CurrentDateTime;
                IsRunning = false;
                analyticsEvent = analyticsEvent.With(stoppedRunningEntry: true);
            }

            if (editMode.Value == EditMode.EndTime)
            {
                editMode.OnNext(EditMode.None);
            }
            else
            {
                MinimumDateTime = MinimumStopTime.LocalDateTime;
                MaximumDateTime = MaximumStopTime.LocalDateTime;

                editMode.OnNext(EditMode.EndTime);
            }

            RaisePropertyChanged(nameof(EditedTime));
        }

        private void stopEditingTime()
        {
            if (editMode.Value == EditMode.None)
            {
                return;
            }

            editMode.OnNext(EditMode.None);
        }

        private void onDurationChanged(TimeSpan changedDuration)
        {
            if (IsRunning)
                StartTime = timeService.CurrentDateTime - changedDuration;

            StopTime = StartTime + changedDuration;
        }

        private void onPreferencesChanged(IThreadSafePreferences preferences)
        {
            durationFormat = preferences.DurationFormat;
            DateFormat = preferences.DateFormat;
            TimeFormat = preferences.TimeOfDayFormat;

            RaisePropertyChanged(nameof(DurationFormat));
        }

        public override void ViewDestroy(bool viewFinishing)
        {
            base.ViewDestroy(viewFinishing);
            runningTimeEntryDisposable?.Dispose();
            preferencesDisposable?.Dispose();
        }

        private enum EditMode
        {
            None,
            StartTime,
            EndTime
        }
    }
}
