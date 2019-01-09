using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Extensions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Interactors
{
    public sealed class ObserveTimeTrackedTodayInteractor : IInteractor<IObservable<TimeSpan>>
    {
        private readonly ITimeService timeService;
        private readonly ITimeEntriesSource timeEntries;

        public ObserveTimeTrackedTodayInteractor(
            ITimeService timeService,
            ITimeEntriesSource timeEntries)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(timeEntries, nameof(timeEntries));

            this.timeService = timeService;
            this.timeEntries = timeEntries;
        }

        public IObservable<TimeSpan> Execute()
            => updateIsNecessary()
                .StartWith(Unit.Default)
                .SelectMany(_ =>
                    calculateTimeAlreadyTrackedToday().CombineLatest(
                        observeElapsedTimeOfCurrentlyRunningTimeEntry(),
                        (alreadyTrackedToday, currentlyRunningTimeEntryDuration) =>
                            alreadyTrackedToday + currentlyRunningTimeEntryDuration))
                .DistinctUntilChanged();

        private IObservable<Unit> updateIsNecessary()
            => timeEntries.ItemsChanged()
                .Merge(timeService.MidnightObservable.SelectUnit())
                .Merge(timeService.SignificantTimeChangeObservable);

        private IObservable<TimeSpan> calculateTimeAlreadyTrackedToday()
            => timeEntries.GetAll(timeEntry =>
                    timeEntry.Start.LocalDateTime.Date == timeService.CurrentDateTime.LocalDateTime.Date
                    && timeEntry.Duration != null)
                .SingleAsync()
                .SelectMany(CommonFunctions.Identity)
                .Sum(timeEntry => timeEntry.Duration ?? 0.0)
                .Select(TimeSpan.FromSeconds);

        private IObservable<TimeSpan> observeElapsedTimeOfCurrentlyRunningTimeEntry()
            => timeEntries.GetAll(timeEntry =>
                    timeEntry.Start.LocalDateTime.Date == timeService.CurrentDateTime.LocalDateTime.Date
                    && timeEntry.Duration == null)
                .Select(runningTimeEntries => runningTimeEntries.SingleOrDefault())
                .SelectMany(timeEntry => timeEntry == null
                    ? Observable.Return(TimeSpan.Zero)
                    : timeService.CurrentDateTimeObservable
                        .Select(now => now - timeEntry.Start)
                        .StartWith(timeService.CurrentDateTime - timeEntry.Start));
    }
}
