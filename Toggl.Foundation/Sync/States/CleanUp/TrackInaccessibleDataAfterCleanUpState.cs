using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Extensions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Sync.States.CleanUp
{
    public sealed class TrackInaccessibleDataAfterCleanUpState : ISyncState
    {
        private readonly ITogglDataSource dataSource;
        private readonly IAnalyticsService analyticsService;

        public StateResult Done { get; } = new StateResult();

        public TrackInaccessibleDataAfterCleanUpState(ITogglDataSource dataSource, IAnalyticsService analyticsService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));

            this.dataSource = dataSource;
            this.analyticsService = analyticsService;
        }

        public IObservable<ITransition> Start()
            => dataSource
                .Workspaces
                .GetAll(ws => ws.IsInaccessible, includeInaccessibleEntities: true)
                .Select(data => data.Count())
                .SelectMany(trackDataIfNeeded)
                .ToList()
                .SelectValue(Done.Transition());

        private IObservable<Unit> trackDataIfNeeded(int numberOfInaccessibleWorkspaces)
        {
            if (numberOfInaccessibleWorkspaces == 0)
            {
                return Observable.Return(Unit.Default);
            }

            var tagsObservable = dataSource.Tags
                .GetAll(tag => tag.IsInaccessible, includeInaccessibleEntities: true)
                .Select(data => data.Count())
                .Track(analyticsService.TagsInaccesibleAfterCleanUp);

            var timeEntriesObservable = dataSource.TimeEntries
                .GetAll(te => te.IsInaccessible, includeInaccessibleEntities: true)
                .Select(data => data.Count())
                .Track(analyticsService.TimeEntriesInaccesibleAfterCleanUp);

            var tasksObservable = dataSource.Tasks
                .GetAll(task => task.IsInaccessible, includeInaccessibleEntities: true)
                .Select(data => data.Count())
                .Track(analyticsService.TasksInaccesibleAfterCleanUp);

            var clientsObservable = dataSource.Clients
                .GetAll(client => client.IsInaccessible, includeInaccessibleEntities: true)
                .Select(data => data.Count())
                .Track(analyticsService.ClientsInaccesibleAfterCleanUp);

            var projectsObservable = dataSource.Projects
                .GetAll(project => project.IsInaccessible, includeInaccessibleEntities: true)
                .Select(data => data.Count())
                .Track(analyticsService.ProjectsInaccesibleAfterCleanUp);

            return Observable
                .Merge(
                    tagsObservable.SelectUnit(),
                    timeEntriesObservable.SelectUnit(),
                    tasksObservable.SelectUnit(),
                    clientsObservable.SelectUnit(),
                    projectsObservable.SelectUnit())
                .SelectUnit();
        }
    }
}

