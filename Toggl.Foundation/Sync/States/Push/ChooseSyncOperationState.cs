using System;
using System.Reactive.Linq;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Sync.States.Push
{
    internal sealed class ChooseSyncOperationState<T> : ISyncState<T>
        where T : class, IDatabaseSyncable, IThreadSafeModel
    {
        public StateResult<T> CreateEntity { get; } = new StateResult<T>();

        public StateResult<T> DeleteEntity { get; } = new StateResult<T>();

        public StateResult<T> UpdateEntity { get; } = new StateResult<T>();

        public StateResult<T> DeleteEntityLocally { get; } = new StateResult<T>();

        public IObservable<ITransition> Start(T entityToPush)
            => createObservable(entityToPush)
                .Select(entity =>
                    entity.IsDeleted
                        ? entity.IsLocalOnly()
                            ? deleteLocally(entity)
                            : delete(entity)
                        : entity.IsLocalOnly()
                            ? create(entity)
                            : update(entity));

        private IObservable<T> createObservable(T entity)
            => entity == null
                ? Observable.Throw<T>(new ArgumentNullException(nameof(entity)))
                : Observable.Return(entity);

        private ITransition delete(T entity) => DeleteEntity.Transition(entity);

        private ITransition create(T entity) => CreateEntity.Transition(entity);

        private ITransition update(T entity) => UpdateEntity.Transition(entity);

        private ITransition deleteLocally(T entity) => DeleteEntityLocally.Transition(entity);
    }
}
