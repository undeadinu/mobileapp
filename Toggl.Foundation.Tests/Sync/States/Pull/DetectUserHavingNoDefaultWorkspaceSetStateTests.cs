using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Sync.States.Pull;
using Toggl.Foundation.Tests.Mocks;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.Pull
{
    public sealed class DetectUserHavingNoDefaultWorkspaceSetStateTests
    {
        private readonly ITogglDataSource dataSource = Substitute.For<ITogglDataSource>();
        private readonly IAnalyticsService analyticsService = Substitute.For<IAnalyticsService>();
        private readonly DetectUserHavingNoDefaultWorkspaceSetState setState;

        public DetectUserHavingNoDefaultWorkspaceSetStateTests()
        {
            setState = new DetectUserHavingNoDefaultWorkspaceSetState(dataSource, analyticsService);
        }

        [Fact, LogIfTooSlow]
        public async Task ReturnsContinueStateWhenUserHasDefeaultWorkspace()
        {
            setupUserWithDefaultWorkspace(1);

            var transition = await setState.Start();

            transition.Result.Should().BeSameAs(setState.Done);
        }

        [Fact, LogIfTooSlow]
        public async Task ReturnsNoDefaultWorkspaceStateWhenUserDoesNotHaveDefeaultWorkspace()
        {
            setupUserWithDefaultWorkspace(null);

            var transition = await setState.Start();

            transition.Result.Should().BeSameAs(setState.NoDefaultWorkspaceDetected);
        }

        [Fact, LogIfTooSlow]
        public async Task TracksTheNoDefaultWorkspaceEventIfThereIsNoDefaultWorkspace()
        {
            setupUserWithDefaultWorkspace(null);

            await setState.Start();

            analyticsService.NoDefaultWorkspace.Received().Track();
        }

        [Fact, LogIfTooSlow]
        public async Task DoesNotTrackTheNoDefaultWorkspaceEventIfThereIsDefaultWorkspace()
        {
            setupUserWithDefaultWorkspace(666);

            await setState.Start();

            analyticsService.NoDefaultWorkspace.DidNotReceive().Track();
        }

        private void setupUserWithDefaultWorkspace(long? defaultWorkspaceId)
        {
            var user = new MockUser { DefaultWorkspaceId = defaultWorkspaceId };

            dataSource.User.Get().Returns(Observable.Return(user));
        }
    }
}
