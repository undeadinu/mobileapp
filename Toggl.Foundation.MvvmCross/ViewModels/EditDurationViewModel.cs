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
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Transformations;
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
        private readonly IAnalyticsService analyticsService;

        private IDisposable runningTimeEntryDisposable;
        private IDisposable preferencesDisposable;

        private DurationParameter defaultResult;

        private EditDurationEvent analyticsEvent;

        public bool IsDurationInitiallyFocused { get; private set; }

        public DateTimeOffset EditedTime
        {
            get
            {
                switch (editMode.Value)
                {
                    case EditMode.StartTime:
                        return startTime.Value;

                    case EditMode.EndTime:
                        return stopTime.Value;

                    default:
                        // any value between start and end time can be returned here
                        // this constraint is to avoid invalid dates with the date picker
                        return startTime.Value;
                }
            }

            set
            {
                if (editMode.Value == EditMode.None) return;

                var valueInRange = value.Clamp(minimumDateTime.Value, maximumDateTime.Value);

                switch (editMode.Value)
                {
                    case EditMode.StartTime:
                        startTime.OnNext(valueInRange);
                        break;

                    case EditMode.EndTime:
                        stopTime.OnNext(valueInRange);
                        break;
                }
            }
        }

        private Subject<Unit> startTimeChangingSubject = new Subject<Unit>();
        public IObservable<Unit> StartTimeChanging
            => startTimeChangingSubject.AsObservable();








        private BehaviorSubject<DateTimeOffset> startTime = new BehaviorSubject<DateTimeOffset>(default(DateTimeOffset));
        private BehaviorSubject<DateTimeOffset> stopTime = new BehaviorSubject<DateTimeOffset>(default(DateTimeOffset));
        private BehaviorSubject<EditMode> editMode = new BehaviorSubject<EditMode>(EditMode.None);
        private BehaviorSubject<bool> isRunning = new BehaviorSubject<bool>(false);

        private BehaviorSubject<DateTime> minimumDateTime = new BehaviorSubject<DateTime>(default(DateTime));
        private BehaviorSubject<DateTime> maximumDateTime = new BehaviorSubject<DateTime>(default(DateTime));

        public UIAction Save { get; }
        public UIAction Close { get; }
        public UIAction EditStartTime { get; }
        public UIAction EditStopTime { get; }
        public UIAction StopEditingTime { get; }
        public InputAction<DateTimeOffset> ChangeStartTime { get; }
        public InputAction<DateTimeOffset> ChangeStopTime { get; }
        public InputAction<TimeSpan> ChangeDuration { get; }

        public IObservable<DateTimeOffset> StartTime { get; }
        public IObservable<DateTimeOffset> StopTime { get; }
        public IObservable<TimeSpan> Duration { get; }
        public IObservable<bool> IsEditingTime { get; }
        public IObservable<bool> IsEditingStartTime { get; }
        public IObservable<bool> IsEditingStopTime { get; }

        public IObservable<string> StartDateString { get; }
        public IObservable<string> StartTimeString { get; }
        public IObservable<string> StopDateString { get; }
        public IObservable<string> StopTimeString { get; }
        public IObservable<string> DurationString { get; }
        public IObservable<TimeFormat> TimeFormat { get; }
        public IObservable<bool> IsRunning { get; }

        public IObservable<DateTime> MinimumDateTime { get; }
        public IObservable<DateTime> MaximumDateTime { get; }

        public IObservable<DateTimeOffset> MinimumStartTime { get; }
        public IObservable<DateTimeOffset> MaximumStartTime { get; }
        public IObservable<DateTimeOffset> MinimumStopTime { get; }
        public IObservable<DateTimeOffset> MaximumStopTime { get; }

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
            this.analyticsService = analyticsService;

            Save = rxActionFactory.FromAsync(save);
            Close = rxActionFactory.FromAsync(close);
            EditStartTime = rxActionFactory.FromAction(editStartTime);
            EditStopTime = rxActionFactory.FromAction(editStopTime);
            StopEditingTime = rxActionFactory.FromAction(stopEditingTime);
            ChangeStartTime = rxActionFactory.FromAction<DateTimeOffset>(startTime.OnNext);
            ChangeStopTime = rxActionFactory.FromAction<DateTimeOffset>(stopTime.OnNext);
            ChangeDuration = rxActionFactory.FromAction<TimeSpan>(changeDuration);

            var start = startTime.Where(v => v != default(DateTimeOffset));
            var stop = stopTime.Where(v => v != default(DateTimeOffset));
            var duration = Observable.CombineLatest(start, stop, (startValue, stopValue) => stopValue - startValue);

            StartTime = start.AsDriver(schedulerProvider);
            StopTime = stop.AsDriver(schedulerProvider);
            Duration = duration.AsDriver(schedulerProvider);

            IsEditingTime = editMode.Select(v => v != EditMode.None).AsDriver(schedulerProvider);
            IsEditingStartTime = editMode.Select(v => v == EditMode.StartTime).AsDriver(schedulerProvider);
            IsEditingStopTime = editMode.Select(v => v == EditMode.EndTime).AsDriver(schedulerProvider);

            var preferences = dataSource.Preferences.Current.ShareReplay();
            var dateFormat = preferences.Select(p => p.DateFormat);
            var timeFormat = preferences.Select(p => p.TimeOfDayFormat);
            var durationFormat = preferences.Select(p => p.DurationFormat);

            StartDateString = Observable.CombineLatest(start, dateFormat, toFormattedString)
                .AsDriver(schedulerProvider);
            StartTimeString = Observable.CombineLatest(start, timeFormat, toFormattedString)
                .AsDriver(schedulerProvider);
            StopDateString = Observable.CombineLatest(stop, dateFormat, toFormattedString)
                .AsDriver(schedulerProvider);
            StopTimeString = Observable.CombineLatest(stop, timeFormat, toFormattedString)
                .AsDriver(schedulerProvider);
            DurationString = Observable.CombineLatest(duration, durationFormat, toFormattedString);
            TimeFormat = timeFormat.AsDriver(schedulerProvider);

            IsRunning = isRunning.AsDriver(schedulerProvider);

            MinimumDateTime = minimumDateTime.AsDriver(schedulerProvider);
            MaximumDateTime = maximumDateTime.AsDriver(schedulerProvider);

            MinimumStartTime = stopTime.Select(v => v.AddHours(-MaxTimeEntryDurationInHours)).AsDriver(schedulerProvider);
            MaximumStartTime = stopTime.AsDriver(schedulerProvider);
            MinimumStopTime = startTime.AsDriver(schedulerProvider);
            MaximumStopTime = startTime.Select(v => v.AddHours(MaxTimeEntryDurationInHours)).AsDriver(schedulerProvider);
        }

        public override void Prepare(EditDurationParameters parameter)
        {
            defaultResult = parameter.DurationParam;
            isRunning.OnNext(defaultResult.Duration.HasValue == false);

            analyticsEvent = new EditDurationEvent(isRunning.Value,
                parameter.IsStartingNewEntry
                    ? EditDurationEvent.NavigationOrigin.Start
                    : EditDurationEvent.NavigationOrigin.Edit);

            if (isRunning.Value)
            {
                runningTimeEntryDisposable = timeService.CurrentDateTimeObservable
                           .Subscribe(currentTime => stopTime.OnNext(currentTime));
            }


            var start = parameter.DurationParam.Start;
            var stop = parameter.DurationParam.Duration.HasValue
                ? start + parameter.DurationParam.Duration.Value
                : timeService.CurrentDateTime;

            startTime.OnNext(start);
            stopTime.OnNext(stop);

            minimumDateTime.OnNext(start.DateTime);
            maximumDateTime.OnNext(stop.DateTime);
            IsDurationInitiallyFocused = parameter.IsDurationInitiallyFocused;
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
            var duration = stopTime.Value - startTime.Value;
            var result = DurationParameter.WithStartAndDuration(startTime.Value, isRunning.Value ? (TimeSpan?)null : duration);
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
                minimumDateTime.OnNext(MinimumStartTime.LocalDateTime);
                maximumDateTime.OnNext(MaximumStartTime.LocalDateTime);

                editMode.OnNext(EditMode.StartTime);
            }

            RaisePropertyChanged(nameof(EditedTime));
        }

        private void editStopTime()
        {
            if (isRunning.Value)
            {
                runningTimeEntryDisposable?.Dispose();
                stopTime.OnNext(timeService.CurrentDateTime);
                isRunning.OnNext(false);
                analyticsEvent = analyticsEvent.With(stoppedRunningEntry: true);
            }

            if (editMode.Value == EditMode.EndTime)
            {
                editMode.OnNext(EditMode.None);
            }
            else
            {
                minimumDateTime.OnNext(MinimumStopTime.LocalDateTime);
                maximumDateTime.OnNext(MaximumStopTime.LocalDateTime);

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

        private void changeDuration(TimeSpan changedDuration)
        {
            if (isRunning.Value)
                startTime.OnNext(timeService.CurrentDateTime - changedDuration);

            stopTime.OnNext(startTime.Value + changedDuration);
        }

        private string toFormattedString(DateTimeOffset dateTimeOffset, TimeFormat timeFormat)
        {
            return DateTimeToFormattedString.Convert(dateTimeOffset, timeFormat.Format);
        }

        private string toFormattedString(DateTimeOffset dateTimeOffset, DateFormat dateFormat)
        {
            return DateTimeToFormattedString.Convert(dateTimeOffset, dateFormat.Short);
        }

        private string toFormattedString(TimeSpan timeSpan, DurationFormat format)
        {
            return timeSpan.ToFormattedString(format);
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
