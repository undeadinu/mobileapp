using System;
using System.Reactive.Linq;
using FluentAssertions;
using Xunit;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States.Push;

namespace Toggl.Foundation.Tests.Sync.States
{
    public sealed class ChooseSyncOperationStateTests
    {
        [Fact, LogIfTooSlow]
        public void ThrowsWhenEntityIsNull()
        {
            Action startWithNull = () => createState().Start(null).SingleAsync().Wait();

            startWithNull.Should().Throw<ArgumentNullException>();
        }

        [Fact, LogIfTooSlow]
        public void ReturnsCreateTransitionWhenTheEntityIsNotPublishedAndNotDeleted()
        {
            var state = createState();
            var entity = TestModel.Dirty(-123);

            var transition = state.Start(entity).SingleAsync().Wait();

            transition.Result.Should().Be(state.CreateEntity);
            ((Transition<IThreadSafeTestModel>)transition).Parameter.Should().Be(entity);
        }

        [Fact, LogIfTooSlow]
        public void ReturnsUpdateTransitionWhenTheEntityIsPublishedAndNotDeleted()
        {
            var state = createState();
            var entity = TestModel.Dirty(123);

            var transition = state.Start(entity).SingleAsync().Wait();

            transition.Result.Should().Be(state.UpdateEntity);
            ((Transition<IThreadSafeTestModel>)transition).Parameter.Should().Be(entity);
        }

        [Fact, LogIfTooSlow]
        public void ReturnsDeleteTransitionWhenTheEntityIsPublishedAndIsDeleted()
        {
            var state = createState();
            var entity = TestModel.DirtyDeleted(123);

            var transition = state.Start(entity).SingleAsync().Wait();

            transition.Result.Should().Be(state.DeleteEntity);
            ((Transition<IThreadSafeTestModel>)transition).Parameter.Should().Be(entity);
        }

        [Fact, LogIfTooSlow]
        public void ReturnsDeleteLocallyTransitionWhenTheEntityIsNotPublishedAndIsDeleted()
        {
            var state = createState();
            var entity = TestModel.DirtyDeleted(-123);

            var transition = state.Start(entity).SingleAsync().Wait();

            transition.Result.Should().Be(state.DeleteEntityLocally);
            ((Transition<IThreadSafeTestModel>)transition).Parameter.Should().Be(entity);
        }

        private ChooseSyncOperationState<IThreadSafeTestModel> createState()
            => new ChooseSyncOperationState<IThreadSafeTestModel>();
    }
}
