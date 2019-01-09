using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.States.Pull
{
    public sealed class MarkWorkspacesAsInaccessibleState : ISyncState<MarkWorkspacesAsInaccessibleParams>
    {
        private readonly IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> dataSource;

        public StateResult<IFetchObservables> Done { get; } = new StateResult<IFetchObservables>();

        public MarkWorkspacesAsInaccessibleState(IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> dataSource)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            this.dataSource = dataSource;
        }

        public IObservable<ITransition> Start(MarkWorkspacesAsInaccessibleParams stateParams)
        {
            var workspaces = stateParams.Workspaces;
            var fetchObservables = stateParams.FetchObservables;

            return Observable.Return(workspaces)
                .SelectMany(CommonFunctions.Identity)
                .SelectMany(markAsInaccessible)
                .ToList()
                .SelectValue(Done.Transition(fetchObservables));
        }

        private IObservable<IThreadSafeWorkspace> markAsInaccessible(IThreadSafeWorkspace workspaceToMark)
            => dataSource.Update(workspaceToMark.AsInaccessible());

    }
}
