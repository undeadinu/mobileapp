using System;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.DataSources;

namespace Toggl.Foundation.Sync.States.Pull
{
    internal sealed class DetectNotHavingAccessToAnyWorkspaceState : ISyncState<IFetchObservables>
    {
        public StateResult<IFetchObservables> Done { get; } = new StateResult<IFetchObservables>();

        private readonly ITogglDataSource dataSource;

        public DetectNotHavingAccessToAnyWorkspaceState(ITogglDataSource dataSource)
        {
            this.dataSource = dataSource;
        }

        public IObservable<ITransition> Start(IFetchObservables fetch)
            => dataSource.Workspaces.GetAll()
                .Select(workspaces => workspaces.Any()
                    ? Done.Transition(fetch)
                    : throw new NoWorkspaceException());
    }
}
