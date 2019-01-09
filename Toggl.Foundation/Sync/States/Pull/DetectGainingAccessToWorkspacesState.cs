using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.States.Pull
{
    public sealed class DetectGainingAccessToWorkspacesState : ISyncState<IFetchObservables>
    {
        private readonly IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> dataSource;

        private readonly IAnalyticsService analyticsService;

        private readonly Func<IInteractor<IObservable<bool>>> hasFinishedSyncBefore;

        public StateResult<IFetchObservables> Done { get; } = new StateResult<IFetchObservables>();
        public StateResult<IEnumerable<IWorkspace>> NewWorkspacesDetected { get; } = new StateResult<IEnumerable<IWorkspace>>();

        public DetectGainingAccessToWorkspacesState(
            IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> dataSource,
            IAnalyticsService analyticsService,
            Func<IInteractor<IObservable<bool>>> hasFinishedSyncBefore)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(hasFinishedSyncBefore, nameof(hasFinishedSyncBefore));

            this.dataSource = dataSource;
            this.analyticsService = analyticsService;
            this.hasFinishedSyncBefore = hasFinishedSyncBefore;
        }

        public IObservable<ITransition> Start(IFetchObservables fetch)
            => fetch.GetList<IWorkspace>()
                .CombineLatest(persistedWorkspaces(), workspacesWhichAreNotInDatabase)
                .CombineLatest(hasFinishedSyncBefore().Execute(), (newWorkspaces, hasSynced) =>
                    newWorkspaces.Any() && hasSynced
                        ? newWorkspacesDetected(newWorkspaces)
                        : continueTransition(fetch));

        private IEnumerable<IWorkspace> workspacesWhichAreNotInDatabase(IEnumerable<IWorkspace> fetched, IEnumerable<IWorkspace> stored)
            => fetched.Where(f => stored.None(s => s.Id == f.Id));

        private ITransition newWorkspacesDetected(IEnumerable<IWorkspace> workspaces)
        {
            analyticsService.GainWorkspaceAccess.Track();
            return NewWorkspacesDetected.Transition(workspaces);
        }

        private ITransition continueTransition(IFetchObservables fetch)
            => Done.Transition(fetch);

        private IObservable<IEnumerable<IWorkspace>> persistedWorkspaces()
            => dataSource.GetAll().SelectMany(CommonFunctions.Identity).ToList();
    }
}
