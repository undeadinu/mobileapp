using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Toggl.Foundation.Tests.Sync.Extensions;
using Toggl.Foundation.Tests.Sync.Helpers;
using Toggl.Foundation.Tests.Sync.State;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Models;

namespace Toggl.Foundation.Tests.Sync.Scenarios.NoDefaultWorkspace
{
    public sealed class RestrictsAccessWhenThereAreMultipleWorkspaces
        : ComplexSyncTest
    {
        protected override ServerState ArrangeServerState(ServerState initialServerState)
            => initialServerState.With(
                workspaces: New<IEnumerable<IWorkspace>>.Value(initialServerState.Workspaces.Append(
                    new Workspace
                    {
                        Id = -1,
                        Name = "Second Workspace"
                    })));

        protected override DatabaseState ArrangeDatabaseState(ServerState serverState)
            // setting `at` in the future and `sync needed` status will force the syncing algorithm to keep this
            // local entity in the database when it pulls data from the server and not override it with the default
            // workspace ID which the server entity uses
            => new DatabaseState(
                user: serverState.User.With(defaultWorkspaceId: null, at: serverState.User.At.AddHours(1))
                    .ToSyncable(SyncStatus.SyncNeeded),
                preferences: serverState.Preferences.ToSyncable(),
                workspaces: serverState.Workspaces.ToSyncable());

        protected override void AssertFinalState(AppServices services, ServerState finalServerState, DatabaseState finalDatabaseState)
        {
            services.AccessRestrictionStorageSubsitute
                .Received()
                .SetNoDefaultWorkspaceStateReached(Arg.Is(true));
        }
    }
}
