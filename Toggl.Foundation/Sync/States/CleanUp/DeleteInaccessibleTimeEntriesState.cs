using System;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.States.CleanUp
{
    public class DeleteInaccessibleTimeEntriesState : ISyncState
    {
        private readonly ITimeEntriesSource dataSource;

        public StateResult Done { get; } = new StateResult();

        public DeleteInaccessibleTimeEntriesState(ITimeEntriesSource dataSource)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            this.dataSource = dataSource;
        }

        public IObservable<ITransition> Start()
            => dataSource
                .GetAll(suitableForDeletion, includeInaccessibleEntities: true)
                .SelectMany(dataSource.DeleteAll)
                .Select(_ => Done.Transition());

        private bool suitableForDeletion(IDatabaseTimeEntry timeEntry)
            => timeEntry.IsInaccessible && isSynced(timeEntry);

        private bool isSynced(IDatabaseTimeEntry timeEntry)
            => timeEntry.SyncStatus == SyncStatus.InSync;
    }
}
