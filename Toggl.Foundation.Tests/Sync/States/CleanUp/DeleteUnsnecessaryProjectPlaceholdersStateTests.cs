using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync.States.CleanUp;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.CleanUp
{
    public sealed class DeleteUnsnecessaryProjectPlaceholdersStateTests
    {
        private readonly DeleteUnnecessaryProjectPlaceholdersState state;
        private readonly ITimeEntriesSource timeEntriesDataSource = Substitute.For<ITimeEntriesSource>();
        private readonly IDataSource<IThreadSafeProject, IDatabaseProject> projectsDataSource =
            Substitute.For<IDataSource<IThreadSafeProject, IDatabaseProject>>();

        public DeleteUnsnecessaryProjectPlaceholdersStateTests()
        {
            state = new DeleteUnnecessaryProjectPlaceholdersState(projectsDataSource, timeEntriesDataSource);
        }

        [Fact, LogIfTooSlow]
        public async Task ReturnsSuccessTransitionWhenItDeletesSomeProjects()
        {
            var project = new MockProject { Id = 123, SyncStatus = SyncStatus.RefetchingNeeded };
            projectsDataSource.GetAll(Arg.Any<Func<IDatabaseProject, bool>>())
                .Returns(Observable.Return(new[] { project }));
            timeEntriesDataSource.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                .Returns(Observable.Return(new IThreadSafeTimeEntry[0]));

            var transition = await state.Start();

            transition.Result.Should().Be(state.Done);
        }

        [Fact, LogIfTooSlow]
        public async Task ReturnsSuccessTransitionWhenItHasNoProjectsToDelete()
        {
            projectsDataSource.GetAll(Arg.Any<Func<IDatabaseProject, bool>>())
                .Returns(Observable.Return(new IThreadSafeProject[0]));

            var transition = await state.Start();

            transition.Result.Should().Be(state.Done);
        }

        [Fact, LogIfTooSlow]
        public async Task DeletesProjectPlaceholderWhenItIsNotReferencedByAnyTimeEntry()
        {
            var project = new MockProject { Id = 123, SyncStatus = SyncStatus.RefetchingNeeded };
            projectsDataSource.GetAll(Arg.Any<Func<IDatabaseProject, bool>>())
                .Returns(Observable.Return(new[] { project }));
            timeEntriesDataSource.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                .Returns(Observable.Return(new IThreadSafeTimeEntry[0]));

            await state.Start();

            await projectsDataSource.Received()
                .DeleteAll(Arg.Is<IEnumerable<IThreadSafeProject>>(projects => projects.Count() == 1 && projects.First().Id == project.Id));
        }

        [Fact, LogIfTooSlow]
        public async Task IgnoresProjectPlaceholdersWhichAreReferencedBySomeTimeEntries()
        {
            var project = new MockProject { Id = 123, SyncStatus = SyncStatus.RefetchingNeeded };
            var timeEntry = new MockTimeEntry { ProjectId = project.Id };
            projectsDataSource.GetAll(Arg.Any<Func<IDatabaseProject, bool>>())
                .Returns(Observable.Return(new[] { project }));
            timeEntriesDataSource.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                .Returns(Observable.Return(new[] { timeEntry }));

            await state.Start();

            await projectsDataSource.Received()
                .DeleteAll(Arg.Is<IEnumerable<IThreadSafeProject>>(projects => projects.None()));
        }

        [Fact, LogIfTooSlow]
        public async Task FiltersOutNonPlaceholderProjectsAndProjectReferencedBySomeTimeEntries()
        {
            var projects = new[]
            {
                new MockProject { Id = 1, SyncStatus = SyncStatus.RefetchingNeeded },
                new MockProject { Id = 2, SyncStatus = SyncStatus.RefetchingNeeded },
                new MockProject { Id = 3, SyncStatus = SyncStatus.InSync },
                new MockProject { Id = 4, SyncStatus = SyncStatus.SyncNeeded },
                new MockProject { Id = 5, SyncStatus = SyncStatus.SyncFailed },
                new MockProject { Id = 6, SyncStatus = SyncStatus.RefetchingNeeded }
            };
            projectsDataSource.GetAll(Arg.Any<Func<IDatabaseProject, bool>>()).Returns(callInfo =>
                Observable.Return(projects.Where<IThreadSafeProject>(callInfo.Arg<Func<IDatabaseProject, bool>>())));

            var timeEntries = new[]
            {
                new MockTimeEntry { ProjectId = projects[5].Id }
            };
            timeEntriesDataSource.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>()).Returns(callInfo =>
                Observable.Return(timeEntries.Where<IThreadSafeTimeEntry>(callInfo.Arg<Func<IDatabaseTimeEntry, bool>>())));

            await state.Start();

            await projectsDataSource.Received().DeleteAll(Arg.Is<IEnumerable<IThreadSafeProject>>(deletedProjects =>
                deletedProjects.Count() == 2
                && deletedProjects.Any(project => project.Id == 1)
                && deletedProjects.Any(project => project.Id == 2)));
        }
    }
}
