using System;
using System.Reactive.Linq;
using System.Reactive;
using System.Threading.Tasks;
using MvvmCross.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using System.Linq;
using System.Collections.Generic;
using Toggl.Foundation.Exceptions;
using System.Collections.Immutable;
using MvvmCross.Navigation;
using Toggl.Foundation.Services;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectDefaultWorkspaceViewModel : MvxViewModelResult<Unit>
    {
        private readonly ITogglDataSource dataSource;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly IAccessRestrictionStorage accessRestrictionStorage;
        private readonly IRxActionFactory rxActionFactory;

        public IImmutableList<SelectableWorkspaceViewModel> Workspaces { get; private set; }

        public InputAction<SelectableWorkspaceViewModel> SelectWorkspace { get; }

        public SelectDefaultWorkspaceViewModel(
            ITogglDataSource dataSource,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IAccessRestrictionStorage accessRestrictionStorage,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(accessRestrictionStorage, nameof(accessRestrictionStorage));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.dataSource = dataSource;
            this.interactorFactory = interactorFactory;
            this.navigationService = navigationService;
            this.accessRestrictionStorage = accessRestrictionStorage;
            this.rxActionFactory = rxActionFactory;

            SelectWorkspace = rxActionFactory.FromObservable<SelectableWorkspaceViewModel>(selectWorkspace);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            Workspaces = await dataSource
                .Workspaces
                .GetAll()
                .Do(throwIfThereAreNoWorkspaces)
                .Select(workspaces => workspaces
                    .Select(toSelectable)
                    .ToImmutableList());
        }

        private SelectableWorkspaceViewModel toSelectable(IThreadSafeWorkspace workspace)
            => new SelectableWorkspaceViewModel(workspace, false);

        private IObservable<Unit> selectWorkspace(SelectableWorkspaceViewModel workspace)
            => Observable.DeferAsync(async _ =>
            {
                await interactorFactory.SetDefaultWorkspace(workspace.WorkspaceId).Execute();
                accessRestrictionStorage.SetNoDefaultWorkspaceStateReached(false);
                await navigationService.Close(this, Unit.Default);
                return Observable.Return(Unit.Default);
            });

        private void throwIfThereAreNoWorkspaces(IEnumerable<IThreadSafeWorkspace> workspaces)
        {
            if (workspaces.None())
                throw new NoWorkspaceException("Found no local workspaces. This view model should not be used, when there are no workspaces");
        }
    }
}
