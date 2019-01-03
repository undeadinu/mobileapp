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
    public sealed class InaccessibleEntitiesOutOfSyncAreNotDeletedInCleanUpLoop : ComplexSyncTest
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
                    new MockClient { Id = 1, WorkspaceId = 1, SyncStatus = SyncStatus.SyncNeeded },
                    new MockClient { Id = 2, WorkspaceId = 2, SyncStatus = SyncStatus.SyncFailed },
                    new MockClient { Id = 3, WorkspaceId = 2, SyncStatus = SyncStatus.InSync }
                },
                tags: new[]
                {
                    new MockTag { Id = 1, WorkspaceId = 1, SyncStatus = SyncStatus.SyncNeeded },
                    new MockTag { Id = 2, WorkspaceId = 2, SyncStatus = SyncStatus.SyncFailed },
                    new MockTag { Id = 3, WorkspaceId = 2, SyncStatus = SyncStatus.InSync }
                },
                projects: new[]
                {
                    new MockProject { Id = 1, WorkspaceId = 1, SyncStatus = SyncStatus.SyncNeeded, ClientId = 1 },
                    new MockProject { Id = 2, WorkspaceId = 2, SyncStatus = SyncStatus.SyncFailed, ClientId = 2 },
                    new MockProject { Id = 3, WorkspaceId = 2, SyncStatus = SyncStatus.InSync, ClientId = 3 }
                },
                tasks: new[]
                {
                    new MockTask { Id = 1, WorkspaceId = 1, SyncStatus = SyncStatus.SyncNeeded, ProjectId = 1 },
                    new MockTask { Id = 2, WorkspaceId = 2, SyncStatus = SyncStatus.SyncFailed, ProjectId = 2 },
                    new MockTask { Id = 3, WorkspaceId = 2, SyncStatus = SyncStatus.InSync, ProjectId = 3 }
                },
                timeEntries: new[]
                {
                    new MockTimeEntry { Id = 1, Start = DateTimeOffset.Now - TimeSpan.FromDays(2), Duration = 10 * 60, WorkspaceId = 1, ProjectId = 1, TagIds = new long[] { 1 }, SyncStatus = SyncStatus.SyncNeeded },
                    new MockTimeEntry { Id = 2, Start = DateTimeOffset.Now - TimeSpan.FromDays(1), Duration = 10 * 60, WorkspaceId = 2, ProjectId = 2, TagIds = new long[] { 2 }, SyncStatus = SyncStatus.SyncFailed },
                    new MockTimeEntry { Id = 3, Start = DateTimeOffset.Now - TimeSpan.FromDays(1), Duration = 10 * 60, WorkspaceId = 2, ProjectId = 3, TagIds = new long[] { 3 }, SyncStatus = SyncStatus.InSync }
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

            finalDatabaseState.Workspaces.Should().HaveCount(3)
                .And
                .Contain(ws => ws.Id == finalServerState.DefaultWorkspace.Id && ws.SyncStatus == SyncStatus.InSync && !ws.IsInaccessible)
                .And
                .Contain(ws => ws.Id == 1 && ws.SyncStatus == SyncStatus.InSync && ws.IsInaccessible)
                .And
                .Contain(ws => ws.Id == 2 && ws.SyncStatus == SyncStatus.InSync && ws.IsInaccessible);

            finalDatabaseState.Clients.Should().HaveCount(2).And.OnlyContain(
                client => client.IsInaccessible && client.SyncStatus != SyncStatus.InSync);
            finalDatabaseState.Tags.Should().HaveCount(2).And.OnlyContain(
                tag => tag.IsInaccessible && tag.SyncStatus != SyncStatus.InSync);
            finalDatabaseState.Projects.Should().HaveCount(2).And.OnlyContain(
                project => project.IsInaccessible && project.SyncStatus != SyncStatus.InSync);
            finalDatabaseState.Tasks.Should().HaveCount(2).And.OnlyContain(
                task => task.IsInaccessible && task.SyncStatus != SyncStatus.InSync);
            finalDatabaseState.TimeEntries.Should().HaveCount(2).And.OnlyContain(
                timeEntry => timeEntry.IsInaccessible && timeEntry.SyncStatus != SyncStatus.InSync);
        }
    }
}
