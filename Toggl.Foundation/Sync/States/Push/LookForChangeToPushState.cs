using System;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync.States.Push.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Sync.States.Push
{
    internal sealed class LookForChangeToPushState<TDatabaseModel, TThreadsafeModel> : ILookForChangeToPushState<TThreadsafeModel>
        where TDatabaseModel : IDatabaseSyncable
        where TThreadsafeModel : class, TDatabaseModel, ILastChangedDatable, IThreadSafeModel
    {
        private readonly IDataSource<TThreadsafeModel, TDatabaseModel> dataSource;

        public StateResult<TThreadsafeModel> ChangeFound { get; } = new StateResult<TThreadsafeModel>();

        public StateResult NoMoreChanges { get; } = new StateResult();

        public LookForChangeToPushState(IDataSource<TThreadsafeModel, TDatabaseModel> dataSource)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.dataSource = dataSource;
        }

        public IObservable<ITransition> Start() =>
            getOldestUnsynced()
                .SingleAsync()
                .Select(entity =>
                    entity != null
                        ? (ITransition)ChangeFound.Transition(entity)
                        : NoMoreChanges.Transition());

        private IObservable<TThreadsafeModel> getOldestUnsynced()
            => dataSource
                .GetAll(entity => entity.SyncStatus == SyncStatus.SyncNeeded)
                .SingleAsync()
                .Select(entities => entities
                    .OrderBy(entity => entity.At)
                    .FirstOrDefault());
    }
}
