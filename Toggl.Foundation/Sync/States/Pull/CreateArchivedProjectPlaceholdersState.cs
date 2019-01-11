using System;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Helper;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Multivac.Extensions;
using Toggl.Ultrawave.Exceptions;
using static Toggl.Multivac.Extensions.CommonFunctions;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.States.Pull
{
    public sealed class CreateArchivedProjectPlaceholdersState : IPersistState
    {
        private readonly IDataSource<IThreadSafeProject, IDatabaseProject> dataSource;

        private readonly IAnalyticsService analyticsService;

        public StateResult<IFetchObservables> Done { get; } = new StateResult<IFetchObservables>();

        public CreateArchivedProjectPlaceholdersState(
            IDataSource<IThreadSafeProject, IDatabaseProject> dataSource,
            IAnalyticsService analyticsService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));

            this.dataSource = dataSource;
            this.analyticsService = analyticsService;
        }

        public IObservable<ITransition> Start(IFetchObservables fetch)
            => fetch.GetList<ITimeEntry>()
                .SingleAsync()
                .SelectMany(Identity)
                .Distinct(timeEntry => timeEntry.ProjectId)
                .WhereAsync(hasUnknownProject)
                .SelectMany(createProjectPlaceholder)
                .Count()
                .Track(analyticsService.ProjectPlaceholdersCreated)
                .SelectValue(Done.Transition(fetch));

        private IObservable<bool> hasUnknownProject(ITimeEntry timeEntry)
            => timeEntry.ProjectId.HasValue
                ? dataSource.GetAll(project => project.Id == timeEntry.ProjectId.Value)
                    .SingleAsync()
                    .Select(projects => projects.None())
                : Observable.Return(false);

        private IObservable<IThreadSafeProject> createProjectPlaceholder(ITimeEntry timeEntry)
        {
            var placeholder = Project.Builder.Create(timeEntry.ProjectId.Value)
                .SetName(Resources.InaccessibleProject)
                .SetWorkspaceId(timeEntry.WorkspaceId)
                .SetColor(Helper.Color.NoProject)
                .SetActive(false)
                .SetAt(default(DateTimeOffset))
                .SetSyncStatus(SyncStatus.RefetchingNeeded)
                .Build();

            return dataSource.Create(placeholder);
        }
    }
}
