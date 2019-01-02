using System;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Interactors
{
    internal sealed class GetDefaultWorkspaceInteractor : TrackableInteractor, IInteractor<IObservable<IThreadSafeWorkspace>>
    {
        private readonly ITogglDataSource dataSource;

        public GetDefaultWorkspaceInteractor(ITogglDataSource dataSource, IAnalyticsService analyticsService) : base(analyticsService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.dataSource = dataSource;
        }

        public IObservable<IThreadSafeWorkspace> Execute()
            => dataSource.User
                .Get()
                .SelectMany(user => user.DefaultWorkspaceId.HasValue
                    ? dataSource.Workspaces.GetById(user.DefaultWorkspaceId.Value)
                    : chooseWorkspace())
                .Catch((InvalidOperationException exception) => chooseWorkspace())
                .Select(Workspace.From);

        private IObservable<IThreadSafeWorkspace> chooseWorkspace()
            => dataSource.Workspaces.GetAll(workspace => !workspace.IsDeleted)
                .Select(workspaces => workspaces.OrderBy(workspace => workspace.Id))
                .SelectMany(workspaces =>
                    workspaces.None()
                        ? Observable.Never<IThreadSafeWorkspace>()
                        : Observable.Return(workspaces.First()));

    }
}
