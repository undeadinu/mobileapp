using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Sync.States.CleanUp;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.CleanUp
{
    public sealed class TrackInaccessibleDataAfterCleanUpStateTests
    {
        public sealed class TheConstructor
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource, bool useAnalyticsService)
            {
                Action tryingToConstructWithNulls = () => new TrackInaccessibleDataAfterCleanUpState(
                    useDataSource ? Substitute.For<ITogglDataSource>() : null,
                    useAnalyticsService ? Substitute.For<IAnalyticsService>() : null
                );

                tryingToConstructWithNulls.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheStartMethod
        {
            private readonly ITogglDataSource dataSource = Substitute.For<ITogglDataSource>();

            private IAnalyticsService analyticsService { get; } = Substitute.For<IAnalyticsService>();

            private readonly MockWorkspace inaccessibleWorkspace = new MockWorkspace { Id = 1, IsInaccessible = true };
            private readonly MockWorkspace accessibleWorkspace = new MockWorkspace { Id = 2, IsInaccessible = false };

            [Fact]
            public async Task TracksInaccessibleDataThatAreRelatedToInaccessibleWorkspaces()
            {
                //tags
                var tags = new[] {
                    new MockTag { Id = 2, Workspace = inaccessibleWorkspace, WorkspaceId = inaccessibleWorkspace.Id },
                    new MockTag { Id = 5, Workspace = accessibleWorkspace, WorkspaceId = accessibleWorkspace.Id }
                };

                dataSource.Tags.GetAll(Arg.Any<Func<IDatabaseTag, bool>>(), Arg.Is(true))
                    .Returns(callInfo =>
                    {
                        var predicate = callInfo[0] as Func<IDatabaseTag, bool>;
                        var filteredTags = tags.Where(predicate);
                        return Observable.Return(filteredTags.Cast<IThreadSafeTag>());
                    });

                //tasks
                var tasks = new[] {
                    new MockTask { Id = 2, Workspace = inaccessibleWorkspace, WorkspaceId = inaccessibleWorkspace.Id },
                    new MockTask { Id = 3, Workspace = inaccessibleWorkspace, WorkspaceId = inaccessibleWorkspace.Id },
                    new MockTask { Id = 5, Workspace = accessibleWorkspace, WorkspaceId = accessibleWorkspace.Id },
                    new MockTask { Id = 6, Workspace = accessibleWorkspace, WorkspaceId = accessibleWorkspace.Id }
                };

                dataSource.Tasks.GetAll(Arg.Any<Func<IDatabaseTask, bool>>(), Arg.Is(true))
                    .Returns(callInfo =>
                    {
                        var predicate = callInfo[0] as Func<IDatabaseTask, bool>;
                        var filteredTasks = tasks.Where(predicate);
                        return Observable.Return(filteredTasks.Cast<IThreadSafeTask>());
                    });

                //timeEntries
                var timeEntries = new[] {
                    new MockTimeEntry { Id = 2, Workspace = inaccessibleWorkspace, WorkspaceId = inaccessibleWorkspace.Id },
                    new MockTimeEntry { Id = 5, Workspace = accessibleWorkspace, WorkspaceId = accessibleWorkspace.Id },
                    new MockTimeEntry { Id = 6, Workspace = accessibleWorkspace, WorkspaceId = accessibleWorkspace.Id }
                };

                dataSource.TimeEntries.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>(), Arg.Is(true))
                    .Returns(callInfo =>
                    {
                        var predicate = callInfo[0] as Func<IDatabaseTimeEntry, bool>;
                        var filteredTimeEntries = timeEntries.Where(predicate);
                        return Observable.Return(filteredTimeEntries.Cast<IThreadSafeTimeEntry>());
                    });

                //projects
                var projects = new[] {
                    new MockProject { Id = 5, Workspace = accessibleWorkspace, WorkspaceId = accessibleWorkspace.Id },
                    new MockProject { Id = 6, Workspace = accessibleWorkspace, WorkspaceId = accessibleWorkspace.Id }
                };

                dataSource.Projects.GetAll(Arg.Any<Func<IDatabaseProject, bool>>(), Arg.Is(true))
                    .Returns(callInfo =>
                    {
                        var predicate = callInfo[0] as Func<IDatabaseProject, bool>;
                        var filteredProjects = projects.Where(predicate);
                        return Observable.Return(filteredProjects.Cast<IThreadSafeProject>());
                    });

                //clients
                var clients = new[] {
                    new MockClient { Id = 2, Workspace = inaccessibleWorkspace, WorkspaceId = inaccessibleWorkspace.Id },
                    new MockClient { Id = 3, Workspace = inaccessibleWorkspace, WorkspaceId = inaccessibleWorkspace.Id },
                    new MockClient { Id = 4, Workspace = inaccessibleWorkspace, WorkspaceId = inaccessibleWorkspace.Id },
                    new MockClient { Id = 5, Workspace = accessibleWorkspace, WorkspaceId = accessibleWorkspace.Id },
                    new MockClient { Id = 6, Workspace = accessibleWorkspace, WorkspaceId = accessibleWorkspace.Id }
                };

                dataSource.Clients.GetAll(Arg.Any<Func<IDatabaseClient, bool>>(), Arg.Is(true))
                    .Returns(callInfo =>
                    {
                        var predicate = callInfo[0] as Func<IDatabaseClient, bool>;
                        var filteredClients = clients.Where(predicate);
                        return Observable.Return(filteredClients.Cast<IThreadSafeClient>());
                    });

                //workspaces
                var workspaces = new[] {
                    inaccessibleWorkspace,
                    accessibleWorkspace
                };

                dataSource.Workspaces.GetAll(Arg.Any<Func<IDatabaseWorkspace, bool>>(), Arg.Is(true))
                    .Returns(callInfo =>
                    {
                        var predicate = callInfo[0] as Func<IDatabaseWorkspace, bool>;
                        var filteredWorkspace = workspaces.Where(predicate);
                        return Observable.Return(filteredWorkspace.Cast<IThreadSafeWorkspace>());
                    });

                var state = new TrackInaccessibleDataAfterCleanUpState(dataSource, analyticsService);

                var transition = await state.Start();

                analyticsService.TagsInaccesibleAfterCleanUp.Received().Track(1);
                analyticsService.TasksInaccesibleAfterCleanUp.Received().Track(2);
                analyticsService.TimeEntriesInaccesibleAfterCleanUp.Received().Track(1);
                analyticsService.ProjectsInaccesibleAfterCleanUp.Received().Track(0);
                analyticsService.ClientsInaccesibleAfterCleanUp.Received().Track(3);
            }

            [Fact]
            public async Task ReturnsOnlyOnceEvenWhenMultipleWorkspacesAreInaccessible()
            {
                // workspaces
                var workspaces = new[]
                {
                    new MockWorkspace { Id = 1, IsInaccessible = false },
                    new MockWorkspace { Id = 2, IsInaccessible = true },
                    new MockWorkspace { Id = 3, IsInaccessible = true },
                    new MockWorkspace { Id = 4, IsInaccessible = true },
                };

                dataSource.Workspaces.GetAll(Arg.Any<Func<IDatabaseWorkspace, bool>>(), Arg.Is(true))
                    .Returns(callInfo =>
                    {
                        var predicate = callInfo[0] as Func<IDatabaseWorkspace, bool>;
                        var filteredWorkspace = workspaces.Where(predicate);
                        return Observable.Return(filteredWorkspace.Cast<IThreadSafeWorkspace>());
                    });

                //timeEntries
                var timeEntries = new[]
                {
                    new MockTimeEntry { Id = 11, Workspace = workspaces[0], WorkspaceId = workspaces[0].Id },
                    new MockTimeEntry { Id = 22, Workspace = workspaces[1], WorkspaceId = workspaces[1].Id },
                    new MockTimeEntry { Id = 33, Workspace = workspaces[2], WorkspaceId = workspaces[2].Id },
                    new MockTimeEntry { Id = 44, Workspace = workspaces[3], WorkspaceId = workspaces[3].Id },
                };

                dataSource.TimeEntries.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>(), Arg.Is(true))
                    .Returns(callInfo =>
                    {
                        var predicate = callInfo[0] as Func<IDatabaseTimeEntry, bool>;
                        var filteredTimeEntries = timeEntries.Where(predicate);
                        return Observable.Return(filteredTimeEntries.Cast<IThreadSafeTimeEntry>());
                    });

                var state = new TrackInaccessibleDataAfterCleanUpState(dataSource, analyticsService);
                var transition = await state.Start().SingleAsync();
                transition.Result.Should().Be(state.Done);
            }
        }
    }
}
