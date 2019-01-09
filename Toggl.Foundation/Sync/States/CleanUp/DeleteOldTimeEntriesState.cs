using System;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.States.CleanUp
{
    internal class DeleteOldTimeEntriesState : ISyncState
    {
        private const byte daysInWeek = 7;
        private const byte weeksToQuery = 8;
        private const byte daysToQuery = daysInWeek * weeksToQuery;
        private static readonly TimeSpan thresholdPeriod = TimeSpan.FromDays(daysToQuery);

        public StateResult Done { get; } = new StateResult();

        private readonly ITimeService timeService;
        private readonly IObservableDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> dataSource;

        public DeleteOldTimeEntriesState(ITimeService timeService, IObservableDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> dataSource)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.dataSource = dataSource;
            this.timeService = timeService;
        }

        public IObservable<ITransition> Start() =>
            dataSource
                .GetAll(suitableForDeletion)
                .SelectMany(dataSource.DeleteAll)
                .Select(_ => Done.Transition());

        private bool suitableForDeletion(IDatabaseTimeEntry timeEntry)
            => calculateDelta(timeEntry) > thresholdPeriod
            && isSynced(timeEntry);

        private TimeSpan calculateDelta(IDatabaseTimeEntry timeEntry)
            => timeService.CurrentDateTime - timeEntry.Start;

        private bool isSynced(IDatabaseTimeEntry timeEntry)
            => timeEntry.SyncStatus == SyncStatus.InSync;
    }
}
