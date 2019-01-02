using System;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.ApiClients.Interfaces;
using static Toggl.Foundation.Sync.PushSyncOperation;

namespace Toggl.Foundation.Sync.States.Push
{
    internal sealed class DeleteEntityState<TModel, TDatabaseModel, TThreadsafeModel>
        : BasePushEntityState<TThreadsafeModel>
        where TModel : IIdentifiable
        where TDatabaseModel : class, TModel, IDatabaseSyncable
        where TThreadsafeModel : class, TDatabaseModel, IThreadSafeModel
    {
        private readonly IDeletingApiClient<TModel> api;

        private readonly IDataSource<TThreadsafeModel, TDatabaseModel> dataSource;

        private readonly ILeakyBucket leakyBucket;
        private readonly IRateLimiter limiter;

        public StateResult DeletingFinished { get; } = new StateResult();

        public DeleteEntityState(
            IDeletingApiClient<TModel> api,
            IAnalyticsService analyticsService,
            IDataSource<TThreadsafeModel, TDatabaseModel> dataSource,
            ILeakyBucket leakyBucket,
            IRateLimiter limiter)
            : base(analyticsService)
        {
            Ensure.Argument.IsNotNull(api, nameof(api));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(leakyBucket, nameof(leakyBucket));
            Ensure.Argument.IsNotNull(limiter, nameof(limiter));

            this.api = api;
            this.dataSource = dataSource;
            this.leakyBucket = leakyBucket;
            this.limiter = limiter;
        }

        public override IObservable<ITransition> Start(TThreadsafeModel entity)
        {
            if (!leakyBucket.TryClaimFreeSlot(out var timeToFreeSlot))
                return Observable.Return(PreventOverloadingServer.Transition(timeToFreeSlot));

            return delete(entity)
                .SelectMany(_ => dataSource.Delete(entity.Id))
                .Track(AnalyticsService.EntitySynced, Delete, entity.GetSafeTypeName())
                .Track(AnalyticsService.EntitySyncStatus, entity.GetSafeTypeName(), $"{Delete}:{Resources.Success}")
                .Select(_ => DeletingFinished.Transition())
                .Catch(Fail(entity, Delete));
        }

        private IObservable<Unit> delete(TModel entity)
            => entity == null
                ? Observable.Throw<Unit>(new ArgumentNullException(nameof(entity)))
                : limiter.WaitForFreeSlot()
                    .ThenExecute(() => api.Delete(entity));
    }
}
