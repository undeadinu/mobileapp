using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class NoWorkspaceViewModel : MvxViewModelResult<Unit>
    {
        private readonly ITogglDataSource dataSource;
        private readonly IAccessRestrictionStorage accessRestrictionStorage;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IRxActionFactory rxActionFactory;

        private readonly Subject<bool> isLoading = new Subject<bool>();

        public IObservable<bool> IsLoading { get; }

        public UIAction CreateWorkspaceWithDefaultName { get; }

        public UIAction TryAgain { get; }

        public NoWorkspaceViewModel(
            ITogglDataSource dataSource,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IAccessRestrictionStorage accessRestrictionStorage,
            ISchedulerProvider schedulerProvider,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(accessRestrictionStorage, nameof(accessRestrictionStorage));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.dataSource = dataSource;
            this.accessRestrictionStorage = accessRestrictionStorage;
            this.navigationService = navigationService;
            this.interactorFactory = interactorFactory;
            this.rxActionFactory = rxActionFactory;

            CreateWorkspaceWithDefaultName = rxActionFactory.FromAsync(createWorkspaceWithDefaultName);
            TryAgain = rxActionFactory.FromAsync(tryAgain);
            IsLoading = isLoading.AsDriver(onErrorJustReturn: false, schedulerProvider: schedulerProvider);
        }

        private async Task tryAgain()
        {
            isLoading.OnNext(true);

            dataSource.CreateNewSyncManager();

            var anyWorkspaceIsAvailable = await dataSource.SyncManager.ForceFullSync()
                .Where(state => state == SyncState.Sleep)
                .SelectMany(_ => interactorFactory.GetAllWorkspaces().Execute())
                .Any(workspaces => workspaces.Any());

            isLoading.OnNext(false);

            if (anyWorkspaceIsAvailable)
            {
                close();
            }
        }

        private async Task createWorkspaceWithDefaultName()
        {
            isLoading.OnNext(true);

            dataSource.CreateNewSyncManager();

            await interactorFactory.CreateDefaultWorkspace().Execute();

            await dataSource
                .SyncManager
                .ForceFullSync();

            isLoading.OnNext(false);
            close();
        }

        private void close()
        {
            accessRestrictionStorage.SetNoWorkspaceStateReached(false);
            navigationService.Close(this, Unit.Default);
        }
    }
}
