using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States.Push;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.Push
{
    public sealed class MarkEntityAsUnsyncableStateTests
    {
        private readonly IBaseDataSource<IThreadSafeTestModel> dataSource
            = Substitute.For<IBaseDataSource<IThreadSafeTestModel>>();

        [Theory, LogIfTooSlow]
        [ConstructorData]
        public void ThrowsWhenArgumentsAreNull(bool hasEntity, bool hasReason)
        {
            var entity = hasEntity ? new TestModel(-1, SyncStatus.SyncNeeded) : (IThreadSafeTestModel)null;
            Exception reason = hasReason ? new ApiException(request, response, "Test.") : null;
            var state = new MarkEntityAsUnsyncableState<IThreadSafeTestModel>(dataSource, TestModel.Unsyncable);

            Action callingStart = () => state.Start((reason, entity)).SingleAsync().Wait();

            callingStart.Should().Throw<ArgumentNullException>();
        }

        [Fact, LogIfTooSlow]
        public void ThrowsWhenDatabaseOperationFails()
        {
            var state = new MarkEntityAsUnsyncableState<IThreadSafeTestModel>(dataSource, TestModel.Unsyncable);
            dataSource
                .OverwriteIfOriginalDidNotChange(null, null)
                .ReturnsForAnyArgs(_ => throw new TestException());

            Action callingStart = () => state.Start(
                (new ApiException(request, response, "Test."), new TestModel(1, SyncStatus.SyncNeeded))).SingleAsync().Wait();

            callingStart.Should().Throw<TestException>();
        }

        [Fact, LogIfTooSlow]
        public void ThrowsWhenTheReasonExceptionIsNotAnApiException()
        {
            var state = new MarkEntityAsUnsyncableState<IThreadSafeTestModel>(dataSource, TestModel.Unsyncable);
            var exception = new TestException();

            Action callingStart = () => state.Start(
                (exception, new TestModel(1, SyncStatus.SyncNeeded))).SingleAsync().Wait();

            callingStart.Should().Throw<TestException>().Where(e => e == exception);
        }

        [Property]
        public void TheErrorMessageMatchesTheMessageFromTheReasonException(NonNull<string> message)
        {
            var entity = new TestModel(1, SyncStatus.SyncNeeded);
            var response = Substitute.For<IResponse>();
            response.RawData.Returns(message.Get);
            var reason = new BadRequestException(request, response);
            var state = new MarkEntityAsUnsyncableState<IThreadSafeTestModel>(dataSource, TestModel.Unsyncable);
            prepareBatchUpdate(entity);

            var transition = state.Start((reason, entity)).SingleAsync().Wait();
            var unsyncableEntity = ((Transition<IThreadSafeTestModel>)transition).Parameter;

            unsyncableEntity.LastSyncErrorMessage.Should().Be(message.Get);
        }

        [Fact, LogIfTooSlow]
        public async Task TheSyncStatusOfTheEntityChangesToSyncFailedWhenEverythingWorks()
        {
            var entity = new TestModel(1, SyncStatus.SyncNeeded);
            var state = new MarkEntityAsUnsyncableState<IThreadSafeTestModel>(dataSource, TestModel.Unsyncable);
            prepareBatchUpdate(entity);

            var transition = await state.Start((new BadRequestException(request, response), entity)).SingleAsync();
            var unsyncableEntity = ((Transition<IThreadSafeTestModel>)transition).Parameter;

            unsyncableEntity.SyncStatus.Should().Be(SyncStatus.SyncFailed);
        }

        [Fact, LogIfTooSlow]
        public async Task TheUpdatedEntityHasTheSameIdAsTheOriginalEntity()
        {
            var entity = new TestModel(1, SyncStatus.SyncNeeded);
            var state = new MarkEntityAsUnsyncableState<IThreadSafeTestModel>(dataSource, TestModel.Unsyncable);
            prepareBatchUpdate(entity);

            await state.Start((new BadRequestException(request, response), entity)).SingleAsync();

            await dataSource
                .Received()
                .OverwriteIfOriginalDidNotChange(
                    Arg.Is(entity),
                    Arg.Is<IThreadSafeTestModel>(updatedEntity => updatedEntity.Id == entity.Id));
        }

        [Fact, LogIfTooSlow]
        public void TheOnlyThingThatChangesInTheUnsyncableEntityIsTheSyncStatusAndLastSyncErrorMessage()
        {
            var entity = new TestModel(1, SyncStatus.SyncNeeded);
            var reason = new BadRequestException(request, response);
            var state = new MarkEntityAsUnsyncableState<IThreadSafeTestModel>(dataSource, TestModel.Unsyncable);
            prepareBatchUpdate(entity);

            var transition = state.Start((reason, entity)).SingleAsync().Wait();
            var unsyncableEntity = ((Transition<IThreadSafeTestModel>)transition).Parameter;

            entity.Should().BeEquivalentTo(unsyncableEntity, options
                => options.IncludingProperties()
                    .Excluding(x => x.LastSyncErrorMessage)
                    .Excluding(x => x.SyncStatus));
        }

        private void prepareBatchUpdate(IThreadSafeTestModel entity)
        {
            dataSource.OverwriteIfOriginalDidNotChange(entity, Arg.Any<IThreadSafeTestModel>())
                .Returns(callInfo => Observable.Return(new[]
                {
                    new UpdateResult<IThreadSafeTestModel>(callInfo.ArgAt<IThreadSafeTestModel>(1).Id, callInfo.ArgAt<IThreadSafeTestModel>(1))
                }));
        }

        private static IRequest request => Substitute.For<IRequest>();

        private static IResponse response => Substitute.For<IResponse>();
    }
}
