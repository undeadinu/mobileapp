using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors
{
    public sealed class GetDefaultWorkspaceInteractorTests
    {
        public sealed class TheGetDefaultWorkspaceInteractor : BaseInteractorTests
        {
            [Fact, LogIfTooSlow]
            public async Task ReturnsUsersDefaultWorkspace()
            {
                const long workspaceId = 11;
                var workspace = new MockWorkspace { Id = workspaceId };
                var user = new MockUser { DefaultWorkspaceId = workspaceId };
                DataSource.User.Get().Returns(Observable.Return(user));
                InteractorFactory.GetWorkspaceById(workspaceId)
                    .Execute()
                    .Returns(Observable.Return(workspace));

                var defaultWorkspace = await InteractorFactory.GetDefaultWorkspace().Execute();

                defaultWorkspace.Should().BeEquivalentTo(workspace, options => options.IncludingProperties());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheWorkspaceWithSmallestIdWhenUsersDefaultWorkspaceIsNull()
            {
                const long workspaceId = 11;
                var workspaces = new[] { new MockWorkspace { Id = workspaceId + 2 }, new MockWorkspace { Id = workspaceId }, new MockWorkspace { Id = workspaceId + 1 } };
                var user = new MockUser { DefaultWorkspaceId = null };
                DataSource.User.Get().Returns(Observable.Return(user));
                DataSource.Workspaces.GetAll(Arg.Any<Func<IDatabaseWorkspace, bool>>())
                    .Returns(Observable.Return(workspaces));

                var defaultWorkspace = await InteractorFactory.GetDefaultWorkspace().Execute();

                defaultWorkspace.Id.Should().Be(workspaceId);
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheWorkspaceWithSmallestIdWhenUsersDefaultWorkspaceIsNotInTheDatabase()
            {
                const long workspaceId = 11;
                var workspaces = new[] { new MockWorkspace { Id = workspaceId + 2 }, new MockWorkspace { Id = workspaceId }, new MockWorkspace { Id = workspaceId + 1 } };
                var user = new MockUser { DefaultWorkspaceId = workspaceId + 3 };
                DataSource.User.Get().Returns(Observable.Return(user));
                InteractorFactory.GetWorkspaceById(workspaceId + 3)
                    .Execute()
                    .Returns(Observable.Throw<IThreadSafeWorkspace>(new InvalidOperationException()));
                DataSource.Workspaces.GetAll(Arg.Any<Func<IDatabaseWorkspace, bool>>())
                    .Returns(Observable.Return(workspaces));

                var defaultWorkspace = await InteractorFactory.GetDefaultWorkspace().Execute();

                defaultWorkspace.Id.Should().Be(workspaceId);
            }
        }
    }
}
