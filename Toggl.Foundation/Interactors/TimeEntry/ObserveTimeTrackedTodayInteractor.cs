using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Models;

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
            => whenUpdateIsNecessary()
                .SelectMany(_ => calculateTimeTrackedToday())
                .DistinctUntilChanged();

        private IObservable<Unit> whenUpdateIsNecessary()
            => timeEntries.ItemsChanged()
                .Merge(timeService.MidnightObservable.SelectUnit())
                .Merge(timeService.SignificantTimeChangeObservable)
                .StartWith(Unit.Default);

        private IObservable<TimeSpan> calculateTimeTrackedToday()
            => calculateTimeAlreadyTrackedToday().CombineLatest(
                observeElapsedTimeOfRunningTimeEntryIfAny(),
                (alreadyTrackedToday, currentlyRunningTimeEntryDuration) =>
                    alreadyTrackedToday + currentlyRunningTimeEntryDuration);

        private IObservable<TimeSpan> calculateTimeAlreadyTrackedToday()
            => timeEntries.GetAll(startedTodayAndStopped)
                .SingleAsync()
                .SelectMany(CommonFunctions.Identity)
                .Sum(timeEntry => timeEntry.Duration ?? 0.0)
                .Select(TimeSpan.FromSeconds);

        private IObservable<TimeSpan> observeElapsedTimeOfRunningTimeEntryIfAny()
            => timeEntries.GetAll(startedTodayAndRunning)
                .Select(runningTimeEntries => runningTimeEntries.SingleOrDefault())
                .SelectMany(timeEntry =>
                    timeEntry == null
                        ? Observable.Return(TimeSpan.Zero)
                        : observeElapsedTimeOfRunningTimeEntry(timeEntry));

        private bool startedTodayAndStopped(IDatabaseTimeEntry timeEntry)
            => timeEntry.Start.LocalDateTime.Date == timeService.CurrentDateTime.LocalDateTime.Date
               && timeEntry.Duration != null;

        private bool startedTodayAndRunning(IDatabaseTimeEntry timeEntry)
            => timeEntry.Start.LocalDateTime.Date == timeService.CurrentDateTime.LocalDateTime.Date
                && timeEntry.Duration == null;

        private IObservable<TimeSpan> observeElapsedTimeOfRunningTimeEntry(IThreadSafeTimeEntry timeEntry)
            => timeService.CurrentDateTimeObservable
                .Select(now => now - timeEntry.Start)
                .StartWith(timeService.CurrentDateTime - timeEntry.Start);
    }
}
