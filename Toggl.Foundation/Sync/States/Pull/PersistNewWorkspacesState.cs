using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.States.Pull
{
    public class PersistNewWorkspacesState : ISyncState<IEnumerable<IWorkspace>>
    {
        private readonly IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> dataSource;

        public StateResult Done { get; } = new StateResult();

        public PersistNewWorkspacesState(IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> dataSource)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            this.dataSource = dataSource;
        }

        public IObservable<ITransition> Start(IEnumerable<IWorkspace> workspaces)
            => Observable.Return(workspaces)
                .SelectMany(CommonFunctions.Identity)
                .Select(Workspace.Clean)
                .SelectMany(createOrUpdate)
                .ToList()
                .SelectValue(Done.Transition());

        private IObservable<IThreadSafeWorkspace> createOrUpdate(IThreadSafeWorkspace workspace)
            => dataSource
                .GetAll(ws => ws.Id == workspace.Id, includeInaccessibleEntities: true)
                .SelectMany(stored => stored.None()
                    ? dataSource.Create(workspace)
                    : dataSource.Update(workspace));
    }
}
