using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.ApiClients;
using static Toggl.Foundation.Sync.PushSyncOperation;

namespace Toggl.Foundation.Sync.States.Push
{
    internal sealed class UpdateEntityState<TModel, TThreadsafeModel>
        : BasePushEntityState<TThreadsafeModel>
        where TThreadsafeModel : class, TModel, IDatabaseSyncable, IThreadSafeModel, IIdentifiable
    {
        private readonly IUpdatingApiClient<TModel> api;

        private readonly IBaseDataSource<TThreadsafeModel> dataSource;

        private readonly ILeakyBucket leakyBucket;
        private readonly IRateLimiter limiter;

        private readonly Func<TModel, TThreadsafeModel> convertToThreadsafeModel;

        public StateResult<TThreadsafeModel> EntityChanged { get; } = new StateResult<TThreadsafeModel>();

        public StateResult<TThreadsafeModel> Done { get; } = new StateResult<TThreadsafeModel>();

        public UpdateEntityState(
            IUpdatingApiClient<TModel> api,
            IBaseDataSource<TThreadsafeModel> dataSource,
            IAnalyticsService analyticsService,
            ILeakyBucket leakyBucket,
            IRateLimiter limiter,
            Func<TModel, TThreadsafeModel> convertToThreadsafeModel)
            : base(analyticsService)
        {
            Ensure.Argument.IsNotNull(api, nameof(api));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(convertToThreadsafeModel, nameof(convertToThreadsafeModel));
            Ensure.Argument.IsNotNull(leakyBucket, nameof(leakyBucket));
            Ensure.Argument.IsNotNull(limiter, nameof(limiter));

            this.api = api;
            this.dataSource = dataSource;
            this.convertToThreadsafeModel = convertToThreadsafeModel;
            this.leakyBucket = leakyBucket;
            this.limiter = limiter;
        }

        public override IObservable<ITransition> Start(TThreadsafeModel entity)
        {
            if (!leakyBucket.TryClaimFreeSlot(out var timeToFreeSlot))
                return Observable.Return(PreventOverloadingServer.Transition(timeToFreeSlot));

            return update(entity)
                .Select(convertToThreadsafeModel)
                .SelectMany(tryOverwrite(entity))
                .Track(AnalyticsService.EntitySynced, Update, entity.GetSafeTypeName())
                .Track(AnalyticsService.EntitySyncStatus, entity.GetSafeTypeName(), $"{Update}:{Resources.Success}")
                .Catch(Fail(entity, Update));
        }

        private Func<TThreadsafeModel, IObservable<ITransition>> tryOverwrite(TThreadsafeModel originalEntity)
          => serverEntity
              => dataSource.OverwriteIfOriginalDidNotChange(originalEntity, serverEntity)
                           .SelectMany(results => getCorrectTransitionFromResults(results, originalEntity));

        private IObservable<ITransition> getCorrectTransitionFromResults(
          IEnumerable<IConflictResolutionResult<TThreadsafeModel>> results,
          TThreadsafeModel originalEntity)
        {
            foreach (var result in results)
            {
                switch (result)
                {
                    case UpdateResult<TThreadsafeModel> u when u.OriginalId == originalEntity.Id:
                        return Observable.Return(Done.Transition(extractFrom(result)));

                    case IgnoreResult<TThreadsafeModel> i when i.Id == originalEntity.Id:
                        return Observable.Return(EntityChanged.Transition(originalEntity));
                }
            }
            throw new ArgumentException("Results must contain result with one of the specified ids.");
        }

        private TThreadsafeModel extractFrom(IConflictResolutionResult<TThreadsafeModel> result)
        {
            if (result is UpdateResult<TThreadsafeModel> updateResult)
                return updateResult.Entity;

            throw new ArgumentOutOfRangeException(nameof(result));
        }

        private IObservable<TModel> update(TModel entity)
            => entity == null
                ? Observable.Throw<TModel>(new ArgumentNullException(nameof(entity)))
                : limiter.WaitForFreeSlot()
                    .ThenExecute(() => api.Update(entity));
    }
}
