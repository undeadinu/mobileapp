using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors.Workspace
{
    public sealed class CreateDefaultWorkspaceInteractorTests
    {
        public sealed class TheExecuteMethod : BaseInteractorTests
        {
            [Fact]
            public async Task CreatesNewWorkspaceLocally()
            {
                var user = new MockUser { Fullname = Guid.NewGuid().ToString() };
                DataSource.User.Current.Returns(Observable.Return(user));
                var interactor = InteractorFactory.CreateDefaultWorkspace();

                await interactor.Execute();

                await DataSource.Workspaces.Received().Create(Arg.Is<IThreadSafeWorkspace>(
                    ws => ws.Name == $"{user.Fullname}'s Workspace" && ws.SyncStatus == SyncStatus.SyncNeeded));
            }

            [Fact]
            public async Task ChangesUsersDefaultWorkspace()
            {
                var workspace = new MockWorkspace { Id = 123456 };
                DataSource.Workspaces.Create(Arg.Any<IThreadSafeWorkspace>()).Returns(Observable.Return(workspace));
                var interactor = InteractorFactory.CreateDefaultWorkspace();

                await interactor.Execute();

                await DataSource.User.Received().Update(Arg.Is<IThreadSafeUser>(
                    user => user.DefaultWorkspaceId == workspace.Id));
            }

            [Fact]
            public async Task RunsTheOperationsInCorrectOrder()
            {
                var interactor = InteractorFactory.CreateDefaultWorkspace();

                await interactor.Execute();

                Received.InOrder(() =>
                {
                    DataSource.Workspaces.Create(Arg.Any<IThreadSafeWorkspace>());
                    DataSource.User.Update(Arg.Any<IThreadSafeUser>());
                    DataSource.SyncManager.PushSync();
                    DataSource.SyncManager.ForceFullSync();
                });
            }
        }
    }
}
