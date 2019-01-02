using System;
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
            => timeEntries.ItemsChanged()
                .Merge(timeService.MidnightObservable.SelectUnit())
                .Merge(timeService.SignificantTimeChangeObservable)
                .StartWith(Unit.Default)
                .SelectMany(_ => calculateTimeTrackedToday());

        private IObservable<TimeSpan> calculateTimeTrackedToday()
            => timeEntries.GetAll(timeEntry =>
                    timeEntry.Start.LocalDateTime.Date == timeService.CurrentDateTime.LocalDateTime.Date
                    && timeEntry.Duration != null)
                .SingleAsync()
                .SelectMany(CommonFunctions.Identity)
                .Sum(timeEntry => timeEntry.Duration ?? 0.0)
                .Select(TimeSpan.FromSeconds);
    }
}
