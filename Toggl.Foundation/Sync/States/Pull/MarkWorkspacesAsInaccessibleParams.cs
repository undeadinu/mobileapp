using System.Collections.Generic;
using Toggl.Foundation.Models.Interfaces;

namespace Toggl.Foundation.Sync.States.Pull
{
    public struct MarkWorkspacesAsInaccessibleParams
    {
        public IEnumerable<IThreadSafeWorkspace> Workspaces { get; }
        public IFetchObservables FetchObservables { get; }

        public MarkWorkspacesAsInaccessibleParams(
            IEnumerable<IThreadSafeWorkspace> Workspaces,
            IFetchObservables FetchObservables)
        {
            this.Workspaces = Workspaces;
            this.FetchObservables = FetchObservables;
        }
    }
}
