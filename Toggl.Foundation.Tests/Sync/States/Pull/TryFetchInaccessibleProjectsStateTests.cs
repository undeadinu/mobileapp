using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Helper;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Sync.States.Pull;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.Exceptions;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.Pull
{
    public sealed class TryFetchInaccessibleProjectsStateTests
    {
        private readonly TryFetchInaccessibleProjectsState state;
        private readonly IProjectsApi api = Substitute.For<IProjectsApi>();
        private readonly ITimeService timeService = Substitute.For<ITimeService>();
        private readonly IFetchObservables fetch = Substitute.For<IFetchObservables>();
        private readonly DateTimeOffset now = new DateTimeOffset(2018, 06, 01, 12, 52, 00, TimeSpan.Zero);
        private readonly IDataSource<IThreadSafeProject, IDatabaseProject> dataSource = 
            Substitute.For<IDataSource<IThreadSafeProject, IDatabaseProject>>();

        public TryFetchInaccessibleProjectsStateTests()
        {
            timeService.CurrentDateTime.Returns(now);
            state = new TryFetchInaccessibleProjectsState(dataSource, timeService, api);
        }

        [Fact]
        public async Task ReturnsFinishedPersistingResultWhenThereAreNoProjectsWhichNeedRefetching()
        {
            setStoredProjects(
                new MockProject { WorkspaceId = 1, SyncStatus = SyncStatus.InSync, At = now.AddDays(-23) },
                new MockProject { WorkspaceId = 2, SyncStatus = SyncStatus.SyncNeeded, At = now.AddDays(-10) },
                new MockProject { WorkspaceId = 3, SyncStatus = SyncStatus.SyncFailed, At = now.AddDays(-2) }
            );

            var transition = await state.Start(fetch);

            transition.Result.Should().Be(state.Done);
        }

        [Fact]
        public async Task ReturnsFinishedPersistingResultWhenThereAreNoProjectsWhichNeedRefetchingAndWereNotUpdatedWithinTheLastTwentyFourHours()
        {
            setStoredProjects(
                new MockProject { WorkspaceId = 1, SyncStatus = SyncStatus.RefetchingNeeded, At = now.AddHours(-23) },
                new MockProject { WorkspaceId = 1, SyncStatus = SyncStatus.RefetchingNeeded, At = now.AddHours(-10) },
                new MockProject { WorkspaceId = 1, SyncStatus = SyncStatus.RefetchingNeeded, At = now.AddHours(-2) },
                new MockProject { WorkspaceId = 1, SyncStatus = SyncStatus.RefetchingNeeded, At = now }
            );

            var transition = await state.Start(fetch);

            transition.Result.Should().Be(state.Done);
        }

        [Fact]
        public async Task ReturnsFetchNextResultWhenAProjectWasProcessed()
        {
            var project = new MockProject
            {
                Id = 234,
                WorkspaceId = 987,
                Name = Resources.InaccessibleProject,
                Color = Color.NoProject,
                SyncStatus = SyncStatus.RefetchingNeeded,
                At = now.AddHours(-25)
            };
            setStoredProjects(project);
            api.Search(project.WorkspaceId, Arg.Is<long[]>(ids => ids.Contains(project.Id)))
                .Returns(Observable.Return(new List<IProject> { project }));

            var transition = await state.Start(fetch);

            transition.Result.Should().Be(state.FetchNext);
        }

        [Fact]
        public async Task QueriesApiInBatchesByWorkspaces()
        {
            setStoredProjects(
                new MockProject { Id = 1, WorkspaceId = 1, Name = "A", Color = "#", SyncStatus = SyncStatus.RefetchingNeeded, At = now.AddHours(-35) },
                new MockProject { Id = 2, WorkspaceId = 1, Name = "B", Color = "#", SyncStatus = SyncStatus.RefetchingNeeded, At = now.AddHours(-26) },
                new MockProject { Id = 3, WorkspaceId = 2, Name = "C", Color = "#", SyncStatus = SyncStatus.RefetchingNeeded, At = now.AddHours(-24.1) },
                new MockProject { Id = 4, WorkspaceId = 3, Name = "D", Color = "#", SyncStatus = SyncStatus.RefetchingNeeded, At = now.AddHours(-48) }
            );

            api.Search(Arg.Any<long>(), Arg.Any<long[]>())
                .Returns(Observable.Return(new List<IProject>()));

            await state.Start(fetch);

            await api.Received().Search(1, Arg.Is<long[]>(ids => ids.Contains(1) && ids.Contains(2) && ids.Length == 2));
        }

        [Fact]
        public async Task OverridesLocalDataWhenAProjectIsFoundOnServer()
        {
            var project = new MockProject
            {
                Id = 123,
                WorkspaceId = 456,
                Name = "Abc",
                Color = "#abcabc",
                SyncStatus = SyncStatus.RefetchingNeeded,
                At = now.AddHours(-25)
            };
            setStoredProjects(project);
            api.Search(project.WorkspaceId, Arg.Is<long[]>(ids => ids.Contains(project.Id)))
                .Returns(Observable.Return(new List<IProject> { project }));

            await state.Start(fetch);

            await dataSource.Received()
                .Update(Arg.Is<IThreadSafeProject>(updatedProject => updatedProject.SyncStatus == SyncStatus.InSync));
        }

        [Fact]
        public async Task UpdatesTheAtPropertyWhenTheProjectCannotBeFoundOnServer()
        {
            var project = new MockProject
            {
                Id = 123,
                WorkspaceId = 456,
                Name = Resources.InaccessibleProject,
                Color = Color.NoProject,
                SyncStatus = SyncStatus.RefetchingNeeded,
                At = now.AddHours(-25)
            };
            setStoredProjects(project);
            api.Search(project.WorkspaceId, Arg.Is<long[]>(ids => ids.Contains(project.Id)))
                .Returns(Observable.Return(new List<IProject>()));

            await state.Start(fetch);

            await dataSource.Received()
                .Update(Arg.Is<IThreadSafeProject>(updatedProject =>
                    updatedProject.SyncStatus == SyncStatus.RefetchingNeeded
                    && updatedProject.At == now));
        }

        [Fact]
        public async Task
            OverridesLocalDataOfAllProjectsFoundOnTheServerAndUpdatesTheAtPropertiesOfAllProjectsWhichWereNotFound()
        {
            setStoredProjects(
                new MockProject { Id = 1, WorkspaceId = 1, Name = "A", Color = "#", SyncStatus = SyncStatus.RefetchingNeeded, At = now.AddHours(-35) },
                new MockProject { Id = 2, WorkspaceId = 1, Name = "B", Color = "#", SyncStatus = SyncStatus.RefetchingNeeded, At = now.AddHours(-26) },
                new MockProject { Id = 3, WorkspaceId = 1, Name = "C", Color = "#", SyncStatus = SyncStatus.RefetchingNeeded, At = now.AddHours(-24.1) },
                new MockProject { Id = 4, WorkspaceId = 1, Name = "D", Color = "#", SyncStatus = SyncStatus.RefetchingNeeded, At = now.AddHours(-48) }
            );

            api.Search(1, Arg.Any<long[]>())
                .Returns(Observable.Return(
                    new List<IProject>
                    {
                        new MockProject { Id = 1, WorkspaceId = 1, At = now.AddHours(-1) },
                        new MockProject { Id = 4, WorkspaceId = 1, At = now.AddHours(-2) },
                    }));

            await state.Start(fetch);

            var calls = dataSource.ReceivedCalls();

            await dataSource.Received()
                .Update(Arg.Is<IThreadSafeProject>(project => project.Id == 1 && project.SyncStatus == SyncStatus.InSync));
            await dataSource.Received()
                .Update(Arg.Is<IThreadSafeProject>(project => project.Id == 2 && project.SyncStatus == SyncStatus.RefetchingNeeded));
            await dataSource.Received()
                .Update(Arg.Is<IThreadSafeProject>(project => project.Id == 3 && project.SyncStatus == SyncStatus.RefetchingNeeded));
            await dataSource.Received()
                .Update(Arg.Is<IThreadSafeProject>(project => project.Id == 4 && project.SyncStatus == SyncStatus.InSync));
        }

        [Fact, LogIfTooSlow]
        public void ThrowsWhenTheDeviceIsOffline()
        {
            setStoredProjects(
                new MockProject
                {
                    Id = 1,
                    WorkspaceId = 1,
                    Name = "A",
                    Color = "#",
                    SyncStatus = SyncStatus.RefetchingNeeded,
                    At = now.AddHours(-35)
                });
            api.Search(1, Arg.Any<long[]>())
                .Returns(Observable.Throw<List<IProject>>(new OfflineException(new Exception())));

            Action startingState = () => state.Start(fetch).SingleAsync().Wait();

            startingState.Should().Throw<OfflineException>();
        }

        private void setStoredProjects(params IThreadSafeProject[] projects)
        {
            dataSource.GetAll(Arg.Any<Func<IDatabaseProject, bool>>())
                .Returns(callInfo => Observable.Return(
                    projects.Where<IThreadSafeProject>(callInfo.Arg<Func<IDatabaseProject, bool>>())));
        }
    }
}
