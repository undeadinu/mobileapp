using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States.Push;
using Toggl.PrimeRadiant;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.Push
{
    public sealed class LookForChangeToPushStateTests
    {
        private readonly IDataSource<IThreadSafeTestModel, IDatabaseTestModel> dataSource
            = Substitute.For<IDataSource<IThreadSafeTestModel, IDatabaseTestModel>>();

        [Fact, LogIfTooSlow]
        public void ConstructorThrowsWithNullDataSource()
        {
            Action creatingWithNullArgument = () => new LookForChangeToPushState<IDatabaseTestModel, IThreadSafeTestModel>(null);

            creatingWithNullArgument.Should().Throw<ArgumentNullException>();
        }

        [Fact, LogIfTooSlow]
        public void ReturnsNothingToPushTransitionWhenTheRepositoryReturnsNoEntity()
        {
            var state = new LookForChangeToPushState<IDatabaseTestModel, IThreadSafeTestModel>(dataSource);
            dataSource.GetAll(Arg.Any<Func<IDatabaseTestModel, bool>>())
                .Returns(Observable.Return(new IThreadSafeTestModel[0]));

            var transition = state.Start().SingleAsync().Wait();

            transition.Result.Should().Be(state.NoMoreChanges);
        }

        [Fact, LogIfTooSlow]
        public void ReturnsPushEntityTransitionWhenTheRepositoryReturnsSomeEntity()
        {
            var state = new LookForChangeToPushState<IDatabaseTestModel, IThreadSafeTestModel>(dataSource);
            var entity = new TestModel(1, SyncStatus.SyncNeeded);
            dataSource.GetAll(Arg.Any<Func<IDatabaseTestModel, bool>>())
                .Returns(Observable.Return(new[] { entity }));

            var transition = state.Start().SingleAsync().Wait();
            var parameter = ((Transition<IThreadSafeTestModel>)transition).Parameter;

            transition.Result.Should().Be(state.ChangeFound);
            parameter.Should().BeEquivalentTo(entity, options => options.IncludingProperties());
        }

        [Fact, LogIfTooSlow]
        public void ThrowsWhenRepositoryThrows()
        {
            var state = new LookForChangeToPushState<IDatabaseTestModel, IThreadSafeTestModel>(dataSource);
            dataSource.GetAll(Arg.Any<Func<IDatabaseTestModel, bool>>())
                .Returns(Observable.Throw<IEnumerable<IThreadSafeTestModel>>(new Exception()));

            Action callingStart = () => state.Start().SingleAsync().Wait();

            callingStart.Should().Throw<Exception>();
        }

        [Fact, LogIfTooSlow]
        public void ReturnsPushEntityTransitionWithTheOldestEntity()
        {
            var at = new DateTimeOffset(2017, 9, 1, 12, 34, 56, TimeSpan.Zero);
            var state = new LookForChangeToPushState<IDatabaseTestModel, IThreadSafeTestModel>(dataSource);
            var entity = new TestModel { At = at, SyncStatus = SyncStatus.SyncNeeded };
            var entity2 = new TestModel { At = at.AddDays(-2), SyncStatus = SyncStatus.SyncNeeded };
            var entity3 = new TestModel { At = at.AddDays(-1), SyncStatus = SyncStatus.SyncNeeded };
            dataSource.GetAll(Arg.Any<Func<IDatabaseTestModel, bool>>())
                .Returns(Observable.Return(new[] { entity, entity2, entity3 }));

            var transition = state.Start().SingleAsync().Wait();
            var parameter = ((Transition<IThreadSafeTestModel>)transition).Parameter;

            transition.Result.Should().Be(state.ChangeFound);
            parameter.Should().BeEquivalentTo(entity2, options => options.IncludingProperties());
        }
    }
}
