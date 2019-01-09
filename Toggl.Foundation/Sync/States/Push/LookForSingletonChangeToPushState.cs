using System;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync.States.Push.Interfaces;
using Toggl.Multivac;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Sync.States.Push
{
    internal sealed class LookForSingletonChangeToPushState<T> : ILookForChangeToPushState<T>
        where T : class, IThreadSafeModel, IDatabaseSyncable
    {
        private readonly ISingletonDataSource<T> dataSource;

        public StateResult<T> ChangeFound { get; } = new StateResult<T>();

        public StateResult NoMoreChanges { get; } = new StateResult();

        public LookForSingletonChangeToPushState(ISingletonDataSource<T> dataSource)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.dataSource = dataSource;
        }

        public IObservable<ITransition> Start() =>
            dataSource
                .Get()
                .Where(entity => entity.SyncStatus == SyncStatus.SyncNeeded)
                .SingleOrDefaultAsync()
                .Select(entity =>
                    entity != null
                        ? (ITransition)ChangeFound.Transition(entity)
                        : NoMoreChanges.Transition());
    }
}
