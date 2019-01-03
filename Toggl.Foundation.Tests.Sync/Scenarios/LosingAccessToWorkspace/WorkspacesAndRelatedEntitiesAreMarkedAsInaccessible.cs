using System;
using System.Linq;
using FluentAssertions;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Foundation.Tests.Sync.Extensions;
using Toggl.Foundation.Tests.Sync.Helpers;
using Toggl.Foundation.Tests.Sync.State;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Tests.Sync.Scenarios.LosingAccessToWorkspace
{
    public sealed class WorkspacesAndRelatedEntitiesAreMarkedAsInaccessible : ComplexSyncTest
    {
        protected override ServerState ArrangeServerState(ServerState initialServerState)
            => initialServerState;

        protected override DatabaseState ArrangeDatabaseState(ServerState serverState)
            => new DatabaseState(
                user: serverState.User.ToSyncable(),
                preferences: serverState.Preferences.ToSyncable(),
                workspaces: serverState.Workspaces.ToSyncable().Concat(new[]
                {
                    new MockWorkspace { Id = 1, Name = "Workspace 1", SyncStatus = SyncStatus.SyncNeeded },
                    new MockWorkspace { Id = 2, Name = "Workspace 2", SyncStatus = SyncStatus.SyncNeeded }
                }),
                clients: new[]
                {
                    new MockClient { Id = 1, WorkspaceId = 1, SyncStatus = SyncStatus.SyncNeeded },
                    new MockClient { Id = 2, WorkspaceId = 2, SyncStatus = SyncStatus.SyncNeeded }
                },
                tags: new[]
                {
                    new MockTag { Id = 1, WorkspaceId = 1, SyncStatus = SyncStatus.SyncNeeded },
                    new MockTag { Id = 2, WorkspaceId = 2, SyncStatus = SyncStatus.SyncNeeded }
                },
                projects: new[]
                {
                    new MockProject { Id = 1, WorkspaceId = 1, SyncStatus = SyncStatus.SyncNeeded, ClientId = 1 },
                    new MockProject { Id = 2, WorkspaceId = 2, SyncStatus = SyncStatus.SyncNeeded, ClientId = 2 }
                },
                tasks: new[]
                {
                    new MockTask { Id = 1, WorkspaceId = 1, SyncStatus = SyncStatus.SyncNeeded, ProjectId = 1 },
                    new MockTask { Id = 2, WorkspaceId = 2, SyncStatus = SyncStatus.SyncNeeded, ProjectId = 2 }
                },
                timeEntries: new[]
                {
                    new MockTimeEntry
                    {
                        Id = 1,
                        Start = DateTimeOffset.Now - TimeSpan.FromDays(2),
                        Duration = 10 * 60,
                        WorkspaceId = 1,
                        ProjectId = 1,
                        TagIds = new long[] { 1 },
                        SyncStatus = SyncStatus.SyncNeeded
                    },
                    new MockTimeEntry
                    {
                        Id = 2,
                        Start = DateTimeOffset.Now - TimeSpan.FromDays(1),
                        Duration = 10 * 60,
                        WorkspaceId = 2,
                        ProjectId = 2,
                        TagIds = new long[] { 2 },
                        SyncStatus = SyncStatus.SyncNeeded
                    }
                });

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
                .Contain(ws => ws.Id == 1 && ws.IsInaccessible)
                .And
                .Contain(ws => ws.Id == 2 && ws.IsInaccessible);

            finalDatabaseState.Clients.Should().HaveCount(2).And.OnlyContain(client => client.IsInaccessible);
            finalDatabaseState.Tags.Should().HaveCount(2).And.OnlyContain(tag => tag.IsInaccessible);
            finalDatabaseState.Projects.Should().HaveCount(2).And.OnlyContain(project => project.IsInaccessible);
            finalDatabaseState.Tasks.Should().HaveCount(2).And.OnlyContain(task => task.IsInaccessible);
            finalDatabaseState.TimeEntries.Should().HaveCount(2).And.OnlyContain(timeEntry => timeEntry.IsInaccessible);
        }
    }
}
