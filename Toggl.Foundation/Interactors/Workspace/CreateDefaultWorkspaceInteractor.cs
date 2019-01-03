using System;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Interactors
{
    internal class CreateDefaultWorkspaceInteractor : IInteractor<IObservable<Unit>>
    {
        private readonly IIdProvider idProvider;
        private readonly ITimeService timeService;
        private readonly ISingletonDataSource<IThreadSafeUser> userDataSource;
        private readonly IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> workspaceDataSource;
        private readonly ISyncManager syncManager;

        public CreateDefaultWorkspaceInteractor(
            IIdProvider idProvider,
            ITimeService timeService,
            ISingletonDataSource<IThreadSafeUser> userDataSource,
            IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> workspaceDataSource,
            ISyncManager syncManager)
        {
            Ensure.Argument.IsNotNull(idProvider, nameof(idProvider));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(userDataSource, nameof(userDataSource));
            Ensure.Argument.IsNotNull(workspaceDataSource, nameof(workspaceDataSource));
            Ensure.Argument.IsNotNull(syncManager, nameof(syncManager));

            this.idProvider = idProvider;
            this.timeService = timeService;
            this.userDataSource = userDataSource;
            this.workspaceDataSource = workspaceDataSource;
            this.syncManager = syncManager;
        }

        public IObservable<Unit> Execute()
            => userDataSource.Current
                .FirstAsync()
                .SelectMany(createWorkspace)
                .SelectMany(updateDefaultWorkspace)
                .SelectMany(_ => syncManager.PushSync().LastAsync())
                .SelectMany(_ => syncManager.ForceFullSync().LastAsync())
                .SelectUnit();

        private IObservable<IThreadSafeUser> updateDefaultWorkspace(IThreadSafeWorkspace workspace)
            => userDataSource.Get()
                .Select(user => user.With(workspace.Id))
                .SelectMany(userDataSource.Update);

        private IObservable<IThreadSafeWorkspace> createWorkspace(IThreadSafeUser user)
            => idProvider.GetNextIdentifier()
                .Apply(Workspace.Builder.Create)
                .SetName($"{user.Fullname}'s Workspace")
                .SetAt(timeService.CurrentDateTime)
                .SetSyncStatus(SyncStatus.SyncNeeded)
                .Build()
                .Apply(workspaceDataSource.Create);
    }
}
