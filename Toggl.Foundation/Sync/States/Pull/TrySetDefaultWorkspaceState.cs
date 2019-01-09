using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Sync.States.Pull
{
    public sealed class TrySetDefaultWorkspaceState : ISyncState
    {
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;

        public StateResult Done { get; } = new StateResult();

        public TrySetDefaultWorkspaceState(ITimeService timeService, ITogglDataSource dataSource)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.dataSource = dataSource;
            this.timeService = timeService;
        }

        public IObservable<ITransition> Start()
            => dataSource
                .Workspaces
                .GetAll()
                .Select(getDefaulWorkspaceIfPossible)
                .SelectMany(setDefaultWorkspace)
                .Select(_ => Done.Transition());

        private IThreadSafeWorkspace getDefaulWorkspaceIfPossible(IEnumerable<IThreadSafeWorkspace> workspaces)
            => workspaces.Count() == 1
                ? workspaces.First()
                : throw new NoDefaultWorkspaceException();

        private IObservable<Unit> setDefaultWorkspace(IThreadSafeWorkspace workspace)
            => new SetDefaultWorkspaceInteractor(timeService, dataSource.User, workspace.Id).Execute();
    }
}
