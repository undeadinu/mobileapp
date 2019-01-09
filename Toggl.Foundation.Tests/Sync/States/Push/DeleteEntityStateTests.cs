using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States.Push;
using Toggl.Foundation.Tests.Sync.States.Push.BaseStates;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.ApiClients.Interfaces;
using Xunit;
using static Toggl.Foundation.Sync.PushSyncOperation;

namespace Toggl.Foundation.Tests.Sync.States.Push
{
    public sealed class DeleteEntityStateTests : BasePushEntityStateTests
    {
        private readonly ITestAnalyticsService analyticsService
            = Substitute.For<ITestAnalyticsService>();

        private readonly IDeletingApiClient<ITestModel> api
            = Substitute.For<IDeletingApiClient<ITestModel>>();

        private readonly IDataSource<IThreadSafeTestModel, IDatabaseTestModel> dataSource
            = Substitute.For<IDataSource<IThreadSafeTestModel, IDatabaseTestModel>>();

        protected override PushSyncOperation Operation => PushSyncOperation.Delete;

        public DeleteEntityStateTests()
        {
            SyncAnalyticsExtensions.SearchStrategy = TestSyncAnalyticsExtensionsSearchStrategy;
        }

        [Fact, LogIfTooSlow]
        public void ReturnsSuccessfulTransitionWhenEverythingWorks()
        {
            var state = (DeleteEntityState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel>)CreateState();
            var dirtyEntity = new TestModel(-1, SyncStatus.SyncNeeded);
            api.Delete(Arg.Any<ITestModel>())
                .Returns(Observable.Return(Unit.Default));
            dataSource.Delete(Arg.Any<long>())
                .Returns(Observable.Return(Unit.Default));

            var transition = state.Start(dirtyEntity).SingleAsync().Wait();

            transition.Result.Should().Be(state.Done);
        }

        [Fact, LogIfTooSlow]
        public void CallsDatabaseDeleteOperationWithCorrectParameter()
        {
            var state = CreateState();
            var dirtyEntity = new TestModel(-1, SyncStatus.SyncNeeded);
            api.Delete(dirtyEntity)
                .Returns(Observable.Return(Unit.Default));

            state.Start(dirtyEntity).SingleAsync().Wait();

            dataSource.Received().Delete(dirtyEntity.Id);
        }

        [Fact, LogIfTooSlow]
        public void WaitsForASlotFromTheRateLimiter()
        {
            var scheduler = new TestScheduler();
            var delay = TimeSpan.FromSeconds(1);
            RateLimiter.WaitForFreeSlot().Returns(Observable.Return(Unit.Default).Delay(delay, scheduler));
            var state = (DeleteEntityState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel>)CreateState();
            var entity = new TestModel(-1, SyncStatus.SyncFailed);

            state.Start(entity).Subscribe();

            scheduler.AdvanceBy(delay.Ticks - 1);
            api.DidNotReceive().Delete(Arg.Any<ITestModel>());
            scheduler.AdvanceBy(1);
            api.Received().Delete(Arg.Any<ITestModel>());
        }

        [Fact, LogIfTooSlow]
        public void DoesNotDeleteTheEntityLocallyIfTheApiOperationFails()
        {
            var state = CreateState();
            var dirtyEntity = new TestModel(-1, SyncStatus.SyncNeeded);
            api.Delete(Arg.Any<ITestModel>())
                .Returns(_ => Observable.Throw<Unit>(new TestException()));

            state.Start(dirtyEntity).SingleAsync().Wait();

            dataSource.DidNotReceive().Delete(Arg.Any<long>());
        }

        [Fact, LogIfTooSlow]
        public void MakesApiCallWithCorrectParameter()
        {
            var state = CreateState();
            var dirtyEntity = new TestModel(-1, SyncStatus.SyncNeeded);
            var calledDelete = false;
            api.Delete(Arg.Is(dirtyEntity))
                .Returns(_ =>
                {
                    calledDelete = true;
                    return Observable.Return(Unit.Default);
                });

            state.Start(dirtyEntity).SingleAsync().Wait();

            calledDelete.Should().BeTrue();
        }

        [Fact, LogIfTooSlow]
        public void TracksEntitySyncStatusInCaseOfSuccess()
        {
            var state = CreateState();
            var entity = new TestModel(-1, SyncStatus.SyncFailed);
            api.Delete(entity)
                .Returns(Observable.Return(Unit.Default));

            state.Start(entity).Wait();

            analyticsService.EntitySyncStatus.Received().Track(
                entity.GetSafeTypeName(),
                $"{Delete}:{Resources.Success}");
        }

        [Fact, LogIfTooSlow]
        public void TracksEntitySyncedInCaseOfSuccess()
        {
            var state = CreateState();
            var entity = new TestModel(-1, SyncStatus.SyncFailed);
            api.Delete(entity)
                .Returns(Observable.Return(Unit.Default));

            state.Start(entity).Wait();

            analyticsService.EntitySynced.Received().Track(Delete, entity.GetSafeTypeName());
        }

        [Fact, LogIfTooSlow]
        public void TracksEntitySyncStatusInCaseOfFailure()
        {
            var exception = new Exception();
            var state = CreateState();
            var entity = new TestModel(-1, SyncStatus.SyncFailed);
            api.Delete(entity)
                .Returns(Observable.Return(Unit.Default));
            PrepareApiCallFunctionToThrow(exception);

            state.Start(entity).Wait();

            analyticsService.EntitySyncStatus.Received().Track(
                entity.GetSafeTypeName(),
                $"{Delete}:{Resources.Failure}");
        }

        [Theory, LogIfTooSlow]
        [MemberData(nameof(EntityTypes), MemberType = typeof(BasePushEntityStateTests))]
        public void TracksEntitySyncErrorInCaseOfFailure(Type entityType)
        {
            var exception = new Exception("SomeRandomMessage");
            var entity = (IThreadSafeTestModel)Substitute.For(new[] { entityType }, new object[0]);
            var state = new DeleteEntityState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel>(api, analyticsService, dataSource, LeakyBucket, RateLimiter);
            var expectedMessage = $"{Delete}:{exception.Message}";
            var analyticsEvent = entity.GetType().ToSyncErrorAnalyticsEvent(analyticsService);
            PrepareApiCallFunctionToThrow(exception);

            state.Start(entity).Wait();

            analyticsEvent.Received().Track(expectedMessage);
        }

        protected override BasePushEntityState<IThreadSafeTestModel> CreateState()
        => new DeleteEntityState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel>(api, analyticsService, dataSource, LeakyBucket, RateLimiter);

        protected override void PrepareApiCallFunctionToThrow(Exception e)
        {
            api.Delete(Arg.Any<ITestModel>())
                .Returns(_ => Observable.Throw<Unit>(e));
        }

        protected override void PrepareDatabaseOperationToThrow(Exception e)
        {
            dataSource.Delete(Arg.Any<long>())
                .Returns(_ => Observable.Throw<Unit>(new TestException()));
        }
    }
}
