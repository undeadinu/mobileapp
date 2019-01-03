using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Foundation.Tests.Sync.Extensions;
using Toggl.Foundation.Tests.Sync.Helpers;
using Toggl.Foundation.Tests.Sync.State;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Helpers;

namespace Toggl.Foundation.Tests.Sync.Scenarios.GainingAccessToWorkspace
{
    public sealed class GainAccessToAWorkspaceForTheFirstTimeTest : ComplexSyncTest
    {
        protected override ServerState ArrangeServerState(ServerState initialServerState)
            => initialServerState.With(
                clients: new[] { new MockClient { Id = -1, WorkspaceId = -2, Name = "c1" } },
                tags: new[]
                {
                    new MockTag { Id = -1, WorkspaceId = -2, Name = "t1" },
                    new MockTag { Id = -2, WorkspaceId = -2, Name = "t2" }
                },
                projects: new[]
                {
                    new MockProject
                    {
                        Id = -1,
                        WorkspaceId = -2,
                        ClientId = -1,
                        Name = "p1",
                        Color = Helper.Color.DefaultProjectColors[0],
                        Active = true
                    }
                },
                timeEntries: new[]
                {
                    new MockTimeEntry
                    {
                        Id = -1,
                        Start = DateTimeOffset.Now - TimeSpan.FromDays(2),
                        Duration = 10 * 60,
                        WorkspaceId = -2,
                        ProjectId = -1,
                        TagIds = new long[] { -1, -2 },
                        Description = "te1"
                    },
                    new MockTimeEntry
                    {
                        Id = -2,
                        Start = DateTimeOffset.Now - TimeSpan.FromDays(1),
                        Duration = 10 * 60,
                        WorkspaceId = -2,
                        ProjectId = -1,
                        TagIds = new long[] { -1 },
                        Description = "te2"
                    }
                },
                workspaces: initialServerState.Workspaces.Concat(new[]
                {
                    new MockWorkspace { Id = -2, Name = "ws2" }
                }).ToArray(),
                pricingPlans: New<IDictionary<long, PricingPlans>>.Value(
                    new Dictionary<long, PricingPlans>
                    {
                        [-2] = PricingPlans.StarterAnnual
                    }));

        protected override DatabaseState ArrangeDatabaseState(ServerState serverState)
            => new DatabaseState(
                user: serverState.User.ToSyncable(),
                preferences: serverState.Preferences.ToSyncable(),
                workspaces: new[]
                {
                    serverState.DefaultWorkspace.ToSyncable()
                });

        protected override void AssertFinalState(AppServices services, ServerState finalServerState, DatabaseState finalDatabaseState)
        {
            if (finalServerState.DefaultWorkspace == null)
                throw new NoDefaultWorkspaceException();

            var workspace = finalServerState.Workspaces.Single(w => w.Name == "ws2");

            finalServerState.Workspaces.Should().HaveCount(2);

            finalDatabaseState.Workspaces.Should().HaveCount(2)
                .And
                .Contain(ws => ws.Id == finalServerState.DefaultWorkspace.Id && ws.SyncStatus == SyncStatus.InSync && !ws.IsInaccessible)
                .And
                .Contain(ws => ws.Id == workspace.Id && ws.SyncStatus == SyncStatus.InSync && !ws.IsInaccessible);

            finalDatabaseState.Clients.Should().HaveCount(1).And.OnlyContain(
                client => !client.IsInaccessible && client.SyncStatus == SyncStatus.InSync);
            finalDatabaseState.Tags.Should().HaveCount(2).And.OnlyContain(
                tag => !tag.IsInaccessible && tag.SyncStatus == SyncStatus.InSync);
            finalDatabaseState.Projects.Should().HaveCount(1).And.OnlyContain(
                project => !project.IsInaccessible && project.SyncStatus == SyncStatus.InSync);
            finalDatabaseState.TimeEntries.Should().HaveCount(2).And.OnlyContain(
                timeEntry => !timeEntry.IsInaccessible && timeEntry.SyncStatus == SyncStatus.InSync);
        }
    }
}
