using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Foundation.Tests.Sync.Extensions;
using Toggl.Foundation.Tests.Sync.Helpers;
using Toggl.Foundation.Tests.Sync.State;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Tests.Sync.Scenarios.LosingAccessToWorkspace
{
    public class WorkspacesAndRelatedInSyncInaccessibleEntitiesAreDeletedInCleanUpLoop : ComplexSyncTest
    {
        protected override ServerState ArrangeServerState(ServerState initialServerState)
            => initialServerState;

        protected override DatabaseState ArrangeDatabaseState(ServerState serverState)
            => new DatabaseState(
                user: serverState.User.ToSyncable(),
                preferences: serverState.Preferences.ToSyncable(),
                workspaces: serverState.Workspaces.ToSyncable().Concat(new[]
                {
                    new MockWorkspace { Id = 1, Name = "Workspace 1", IsInaccessible = true, SyncStatus = SyncStatus.InSync },
                    new MockWorkspace { Id = 2, Name = "Workspace 2", IsInaccessible = true, SyncStatus = SyncStatus.InSync }
                }),
                clients: new[]
                {
                    new MockClient { Id = 1, WorkspaceId = 1, SyncStatus = SyncStatus.InSync },
                    new MockClient { Id = 2, WorkspaceId = 2, SyncStatus = SyncStatus.InSync }
                },
                tags: new[]
                {
                    new MockTag { Id = 1, WorkspaceId = 1, SyncStatus = SyncStatus.InSync },
                    new MockTag { Id = 2, WorkspaceId = 2, SyncStatus = SyncStatus.InSync }
                },
                projects: new[]
                {
                    new MockProject { Id = 1, WorkspaceId = 1, SyncStatus = SyncStatus.InSync, ClientId = 1 },
                    new MockProject { Id = 2, WorkspaceId = 2, SyncStatus = SyncStatus.InSync, ClientId = 2 }
                },
                tasks: new[]
                {
                    new MockTask { Id = 1, WorkspaceId = 1, SyncStatus = SyncStatus.InSync, ProjectId = 1 },
                    new MockTask { Id = 2, WorkspaceId = 2, SyncStatus = SyncStatus.InSync, ProjectId = 2 }
                },
                timeEntries: new[]
                {
                    new MockTimeEntry { Id = 1, Start = DateTimeOffset.Now - TimeSpan.FromDays(2), Duration = 10 * 60, WorkspaceId = 1, ProjectId = 1, TagIds = new long[] { 1 }, SyncStatus = SyncStatus.InSync },
                    new MockTimeEntry { Id = 2, Start = DateTimeOffset.Now - TimeSpan.FromDays(1), Duration = 10 * 60, WorkspaceId = 2, ProjectId = 2, TagIds = new long[] { 2 }, SyncStatus = SyncStatus.InSync }
                });

        protected override async Task Act(ISyncManager syncManager)
        {
            await syncManager.CleanUp();
        }

        protected override void AssertFinalState(AppServices services, ServerState finalServerState, DatabaseState finalDatabaseState)
        {
            if (finalServerState.DefaultWorkspace == null)
                throw new NoDefaultWorkspaceException();

            finalServerState.Workspaces.Should().HaveCount(1)
                .And
                .Contain(ws => ws.Id == finalServerState.DefaultWorkspace.Id);

            finalDatabaseState.Workspaces.Should().HaveCount(1)
                .And
                .Contain(ws => ws.Id == finalServerState.DefaultWorkspace.Id);

            finalDatabaseState.Clients.Should().HaveCount(0);
            finalDatabaseState.Tags.Should().HaveCount(0);
            finalDatabaseState.Projects.Should().HaveCount(0);
            finalDatabaseState.Tasks.Should().HaveCount(0);
            finalDatabaseState.TimeEntries.Should().HaveCount(0);
        }
    }
}
