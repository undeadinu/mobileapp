using System;
using System.Reactive;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Sync.States.Push;
using Toggl.PrimeRadiant;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.Push
{
    public sealed class DeleteLocalEntityStateTests
    {
        private readonly IDataSource<IThreadSafeTestModel, IDatabaseTestModel> dataSource
            = Substitute.For<IDataSource<IThreadSafeTestModel, IDatabaseTestModel>>();

        [Fact, LogIfTooSlow]
        public void ReturnsFailTransitionWhenEntityIsNull()
        {
            var state = new DeleteLocalEntityState<IDatabaseTestModel, IThreadSafeTestModel>(dataSource);

            var transition = state.Start(null).SingleAsync().Wait();

            transition.Result.Should().Be(state.DeletingFailed);
        }

        [Fact, LogIfTooSlow]
        public void ReturnsFailTransitionWhenDatabaseOperationFails()
        {
            var state = new DeleteLocalEntityState<IDatabaseTestModel, IThreadSafeTestModel>(dataSource);
            var entity = new TestModel(0, SyncStatus.SyncNeeded);
            dataSource.Delete(entity.Id).Returns(Observable.Throw<Unit>(new Exception()));

            var transition = state.Start(entity).SingleAsync().Wait();

            transition.Result.Should().Be(state.DeletingFailed);
        }

        [Fact, LogIfTooSlow]
        public void ReturnsDeletedTransitionWhenEverythingIsOk()
        {
            var state = new DeleteLocalEntityState<IDatabaseTestModel, IThreadSafeTestModel>(dataSource);
            var entity = new TestModel(0, SyncStatus.SyncNeeded);
            dataSource.Delete(Arg.Any<long>()).Returns(Observable.Return(Unit.Default));

            var transition = state.Start(entity).SingleAsync().Wait();

            transition.Result.Should().Be(state.Done);
        }

        [Fact, LogIfTooSlow]
        public void DeletesTheEntityFromTheLocalDatabase()
        {
            var state = new DeleteLocalEntityState<IDatabaseTestModel, IThreadSafeTestModel>(dataSource);
            var entity = new TestModel(0, SyncStatus.SyncNeeded);
            dataSource.Delete(Arg.Any<long>()).Returns(Observable.Return(Unit.Default));

            state.Start(entity).SingleAsync().Wait();

            dataSource.Received().Delete(Arg.Any<long>());
        }
    }
}
