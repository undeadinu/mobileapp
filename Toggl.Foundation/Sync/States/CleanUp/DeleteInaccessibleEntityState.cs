using System;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.States.CleanUp
{
    public abstract class DeleteInaccessibleEntityState<TInterface, TDatabaseInterface>
        : ISyncState
        where TInterface : class, TDatabaseInterface, IThreadSafeModel
        where TDatabaseInterface : IDatabaseSyncable, IPotentiallyInaccessible
    {
        private readonly IDataSource<TInterface, TDatabaseInterface> dataSource;

        public StateResult Done { get; } = new StateResult();

        public DeleteInaccessibleEntityState(
            IDataSource<TInterface, TDatabaseInterface> dataSource)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            this.dataSource = dataSource;
        }

        public IObservable<ITransition> Start()
            => dataSource.GetAll(candidateForDeletion, includeInaccessibleEntities: true)
                .SelectMany(CommonFunctions.Identity)
                .WhereAsync(SuitableForDeletion)
                .ToList()
                .SelectMany(dataSource.DeleteAll)
                .SelectValue(Done.Transition());

        protected abstract IObservable<bool> SuitableForDeletion(TInterface entity);

        private bool candidateForDeletion(TDatabaseInterface entity)
            => entity.IsInaccessible && entity.SyncStatus == SyncStatus.InSync;
    }
}
