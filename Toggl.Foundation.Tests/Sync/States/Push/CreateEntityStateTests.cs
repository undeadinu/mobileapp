using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
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
using Toggl.Ultrawave.ApiClients;
using Xunit;
using static Toggl.Foundation.Sync.PushSyncOperation;

namespace Toggl.Foundation.Tests.Sync.States.Push
{
    public sealed class CreateEntityStateTests : BasePushEntityStateTests
    {
        private readonly ICreatingApiClient<ITestModel> api
            = Substitute.For<ICreatingApiClient<ITestModel>>();

        private readonly IDataSource<IThreadSafeTestModel, IDatabaseTestModel> dataSource;

        private readonly ITestAnalyticsService analyticsService
            = Substitute.For<ITestAnalyticsService>();

        protected override PushSyncOperation Operation => PushSyncOperation.Create;

        public CreateEntityStateTests()
            : this(Substitute.For<IDataSource<IThreadSafeTestModel, IDatabaseTestModel>>())
        {
        }

        private CreateEntityStateTests(IDataSource<IThreadSafeTestModel, IDatabaseTestModel> dataSource)
        {
            this.dataSource = dataSource;
            SyncAnalyticsExtensions.SearchStrategy = TestSyncAnalyticsExtensionsSearchStrategy;
        }

        [Fact, LogIfTooSlow]
        public void ReturnsSuccessfulTransitionWhenEverythingWorks()
        {
            var state = (CreateEntityState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel>)CreateState();
            var entity = new TestModel(-1, SyncStatus.SyncFailed);
            var withPositiveId = new TestModel(13, SyncStatus.InSync);
            api.Create(Arg.Any<ITestModel>())
                .Returns(Observable.Return(withPositiveId));
            dataSource.OverwriteIfOriginalDidNotChange(Arg.Any<IThreadSafeTestModel>(), Arg.Any<IThreadSafeTestModel>())
                      .Returns(x => Observable.Return(new[] {
                            new UpdateResult<IThreadSafeTestModel>(entity.Id, entityWithId((IThreadSafeTestModel)x[1], entity.Id))
                      }));
            dataSource.ChangeId(entity.Id, withPositiveId.Id).Returns(Observable.Return(withPositiveId));

            var transition = state.Start(entity).SingleAsync().Wait();
            var persistedEntity = ((Transition<IThreadSafeTestModel>)transition).Parameter;

            dataSource.Received().ChangeId(entity.Id, withPositiveId.Id);
            transition.Result.Should().Be(state.Done);
            persistedEntity.Id.Should().Be(withPositiveId.Id);
            persistedEntity.SyncStatus.Should().Be(SyncStatus.InSync);
        }

        [Fact, LogIfTooSlow]
        public void WaitsForASlotFromTheRateLimiter()
        {
            var scheduler = new TestScheduler();
            var delay = TimeSpan.FromSeconds(1);
            RateLimiter.WaitForFreeSlot().Returns(Observable.Return(Unit.Default).Delay(delay, scheduler));
            var state = (CreateEntityState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel>)CreateState();
            var entity = new TestModel(-1, SyncStatus.SyncFailed);

            state.Start(entity).Subscribe();

            scheduler.AdvanceBy(delay.Ticks - 1);
            api.DidNotReceive().Create(Arg.Any<ITestModel>());
            scheduler.AdvanceBy(1);
            api.Received().Create(Arg.Any<ITestModel>());
        }

        private static IThreadSafeTestModel entityWithId(IThreadSafeTestModel testModel, long id)
        {
            var newModel = TestModel.From(testModel);
            newModel.Id = id;
            return newModel;
        }

        [Fact, LogIfTooSlow]
        public void TriesToSafelyOverwriteLocalDataWithDataFromTheServer()
        {
            var state = (CreateEntityState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel>)CreateState();
            var entity = new TestModel(-1, SyncStatus.SyncFailed);
            var withPositiveId = new TestModel(Math.Abs(entity.Id), SyncStatus.InSync);
            api.Create(entity)
                .Returns(Observable.Return(withPositiveId));

            state.Start(entity).SingleAsync().Wait();

            dataSource
                .Received()
                .OverwriteIfOriginalDidNotChange(
                    Arg.Is<IThreadSafeTestModel>(original => original.Id == entity.Id),
                    Arg.Is<IThreadSafeTestModel>(fromServer => fromServer.Id == withPositiveId.Id));
        }

        [Fact, LogIfTooSlow]
        public void UpdateIsCalledWithCorrectParameters()
        {
            var state = (CreateEntityState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel>)CreateState();
            var entity = new TestModel(-1, SyncStatus.SyncFailed);
            var withPositiveId = new TestModel(Math.Abs(entity.Id), SyncStatus.InSync);
            api.Create(entity)
                .Returns(Observable.Return(withPositiveId));

            state.Start(entity).SingleAsync().Wait();

            dataSource
                .Received()
                .OverwriteIfOriginalDidNotChange(Arg.Is<IThreadSafeTestModel>(model => model.Id == entity.Id), Arg.Is<IThreadSafeTestModel>(model => model.Id == withPositiveId.Id));
        }

        [Fact, LogIfTooSlow]
        public void ReturnsTheEntityChangedTransitionWhenEntityChangesLocally()
        {
            var state = (CreateEntityState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel>)CreateState();
            var at = new DateTimeOffset(2017, 9, 1, 12, 34, 56, TimeSpan.Zero);
            var entity = new TestModel { Id = 1, At = at, SyncStatus = SyncStatus.SyncNeeded };
            api.Create(Arg.Any<ITestModel>())
                .Returns(Observable.Return(entity));
            dataSource
                .OverwriteIfOriginalDidNotChange(Arg.Any<IThreadSafeTestModel>(), Arg.Any<IThreadSafeTestModel>())
                .Returns(Observable.Return(new[] { new IgnoreResult<IThreadSafeTestModel>(entity.Id) }));
            dataSource.ChangeId(Arg.Any<long>(), entity.Id).Returns(Observable.Return(entity));

            var transition = state.Start(entity).SingleAsync().Wait();
            var parameter = ((Transition<IThreadSafeTestModel>)transition).Parameter;

            transition.Result.Should().Be(state.EntityChanged);
            parameter.Id.Should().Be(entity.Id);
        }

        [Fact, LogIfTooSlow]
        public void ChangesIdIfTheEntityChangedLocally()
        {
            var state = (CreateEntityState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel>)CreateState();
            var at = new DateTimeOffset(2017, 9, 1, 12, 34, 56, TimeSpan.Zero);
            var entity = new TestModel { Id = -1, At = at, SyncStatus = SyncStatus.SyncNeeded };
            var updatedEntity = new TestModel { Id = 13, At = at, SyncStatus = SyncStatus.SyncNeeded };
            api.Create(Arg.Any<ITestModel>())
                .Returns(Observable.Return(updatedEntity));
            dataSource
                .OverwriteIfOriginalDidNotChange(Arg.Any<IThreadSafeTestModel>(), Arg.Any<IThreadSafeTestModel>())
                .Returns(Observable.Return(new[] { new IgnoreResult<IThreadSafeTestModel>(entity.Id) }));
            dataSource.ChangeId(Arg.Any<long>(), updatedEntity.Id).Returns(Observable.Return(entity));

            _ = state.Start(entity).SingleAsync().Wait();

            dataSource.Received().ChangeId(entity.Id, updatedEntity.Id);
        }

        [Fact, LogIfTooSlow]
        public void TracksEntitySyncStatusInCaseOfSuccess()
        {
            var exception = new Exception();
            var state = CreateState();
            var entity = new TestModel(-1, SyncStatus.SyncFailed);
            var withPositiveId = new TestModel(Math.Abs(entity.Id), SyncStatus.InSync);
            api.Create(Arg.Any<ITestModel>())
               .Returns(Observable.Return(withPositiveId));
            dataSource.OverwriteIfOriginalDidNotChange(Arg.Any<IThreadSafeTestModel>(), Arg.Any<IThreadSafeTestModel>())
                      .Returns(x => Observable.Return(new[] { new UpdateResult<IThreadSafeTestModel>(entity.Id, (IThreadSafeTestModel)x[1]) }));

            state.Start(entity).Wait();

            analyticsService.EntitySyncStatus.Received().Track(
                entity.GetSafeTypeName(),
                $"{Create}:{Resources.Success}");
        }

        [Fact, LogIfTooSlow]
        public void TracksEntitySyncedInCaseOfSuccess()
        {
            var exception = new Exception();
            var state = CreateState();
            var entity = new TestModel(-1, SyncStatus.SyncFailed);
            var withPositiveId = new TestModel(Math.Abs(entity.Id), SyncStatus.InSync);
            api.Create(Arg.Any<ITestModel>())
               .Returns(Observable.Return(withPositiveId));
            dataSource.OverwriteIfOriginalDidNotChange(Arg.Any<IThreadSafeTestModel>(), Arg.Any<IThreadSafeTestModel>())
                      .Returns(x => Observable.Return(new[] { new UpdateResult<IThreadSafeTestModel>(entity.Id, (IThreadSafeTestModel)x[1]) }));

            state.Start(entity).Wait();

            analyticsService.EntitySynced.Received().Track(Create, entity.GetSafeTypeName());
        }

        [Fact, LogIfTooSlow]
        public void TracksEntitySyncStatusInCaseOfFailure()
        {
            var exception = new Exception();
            var state = CreateState();
            var entity = Substitute.For<IThreadSafeTestModel>();
            PrepareApiCallFunctionToThrow(exception);

            state.Start(entity).Wait();

            analyticsService.EntitySyncStatus.Received().Track(
                entity.GetSafeTypeName(),
                $"{Create}:{Resources.Failure}");
        }

        [Theory, LogIfTooSlow]
        [MemberData(nameof(EntityTypes), MemberType = typeof(BasePushEntityStateTests))]
        public void TracksEntitySyncErrorInCaseOfFailure(Type entityType)
        {
            var exception = new Exception("SomeRandomMessage");
            var entity = (IThreadSafeTestModel)Substitute.For(new[] { entityType }, new object[0]);
            var state = new CreateEntityState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel>(api, dataSource, analyticsService, LeakyBucket, RateLimiter, _ => null);
            var expectedMessage = $"{Create}:{exception.Message}";
            var analyticsEvent = entity.GetType().ToSyncErrorAnalyticsEvent(analyticsService);
            PrepareApiCallFunctionToThrow(exception);

            state.Start(entity).Wait();

            analyticsEvent.Received().Track(expectedMessage);
        }

        protected override BasePushEntityState<IThreadSafeTestModel> CreateState()
            => new CreateEntityState<ITestModel, IDatabaseTestModel, IThreadSafeTestModel>(api, dataSource, analyticsService, LeakyBucket, RateLimiter, TestModel.From);

        protected override void PrepareApiCallFunctionToThrow(Exception e)
        {
            api.Create(Arg.Any<ITestModel>())
                .Returns(_ => Observable.Throw<ITestModel>(e));
        }

        protected override void PrepareDatabaseOperationToThrow(Exception e)
        {
            dataSource.OverwriteIfOriginalDidNotChange(Arg.Any<IThreadSafeTestModel>(), Arg.Any<IThreadSafeTestModel>())
                      .Returns(_ => Observable.Throw<IEnumerable<IConflictResolutionResult<IThreadSafeTestModel>>>(e));
        }
    }
}
