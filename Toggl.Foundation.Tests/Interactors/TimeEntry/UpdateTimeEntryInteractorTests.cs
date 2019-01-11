using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync;
using Toggl.PrimeRadiant;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors.TimeEntry
{
    public class UpdateTimeEntryInteractorTests : BaseInteractorTests
    {
        private static DateTimeOffset now = new DateTimeOffset(2018, 05, 14, 18, 00, 00, TimeSpan.Zero);

        private readonly IThreadSafeTimeEntry timeEntry =
            Models.TimeEntry.Builder
                .Create(9)
                .SetUserId(10)
                .SetDescription("")
                .SetWorkspaceId(11)
                .SetSyncStatus(SyncStatus.InSync)
                .SetAt(now.AddDays(-1))
                .SetStart(now.AddHours(-2))
                .Build();

        private EditTimeEntryDto prepareTest()
        {
            var observable = Observable.Return(timeEntry);
            InteractorFactory.GetTimeEntryById(Arg.Is(timeEntry.Id))
                .Execute()
                .Returns(observable);

            var dto = new EditTimeEntryDto
            {
                Id = timeEntry.Id,
                Description = "New description",
                StartTime = DateTimeOffset.UtcNow,
                ProjectId = 13,
                Billable = true,
                WorkspaceId = 71,
                TagIds = new long[] { 1, 10, 34, 42 }
            };

            return dto;
        }

        private bool ensurePropertiesDidNotChange(IThreadSafeTimeEntry otherTimeEntry)
            => timeEntry.Id == otherTimeEntry.Id
            && timeEntry.UserId == otherTimeEntry.UserId
            && timeEntry.IsDeleted == otherTimeEntry.IsDeleted
            && timeEntry.ServerDeletedAt == otherTimeEntry.ServerDeletedAt;

        [Fact, LogIfTooSlow]
        public async Task UpdatesTheDescriptionProperty()
        {
            var dto = prepareTest();

            await new UpdateTimeEntryInteractor(TimeService, DataSource, InteractorFactory, dto).Execute();

            await DataSource.Received().TimeEntries.Update(Arg.Is<IThreadSafeTimeEntry>(te => te.Description == dto.Description));
        }

        [Fact, LogIfTooSlow]
        public async Task UpdatesTheSyncStatusProperty()
        {
            var dto = prepareTest();

            await new UpdateTimeEntryInteractor(TimeService, DataSource, InteractorFactory, dto).Execute();

            await DataSource.Received().TimeEntries.Update(Arg.Is<IThreadSafeTimeEntry>(te => te.SyncStatus == SyncStatus.SyncNeeded));
        }

        [Fact, LogIfTooSlow]
        public async Task UpdatesTheAtProperty()
        {
            var dto = prepareTest();
            TimeService.CurrentDateTime.Returns(DateTimeOffset.UtcNow);

            await new UpdateTimeEntryInteractor(TimeService, DataSource, InteractorFactory, dto).Execute();

            await DataSource.Received().TimeEntries.Update(Arg.Is<IThreadSafeTimeEntry>(te => te.At > timeEntry.At));
        }

        [Fact, LogIfTooSlow]
        public async Task UpdatesTheProjectId()
        {
            var dto = prepareTest();

            await new UpdateTimeEntryInteractor(TimeService, DataSource, InteractorFactory, dto).Execute();

            await DataSource.Received().TimeEntries.Update(Arg.Is<IThreadSafeTimeEntry>(te => te.ProjectId == dto.ProjectId));
        }

        [Fact, LogIfTooSlow]
        public async Task UpdatesTheBillbaleFlag()
        {
            var dto = prepareTest();

            await new UpdateTimeEntryInteractor(TimeService, DataSource, InteractorFactory, dto).Execute();

            await DataSource.Received().TimeEntries.Update(Arg.Is<IThreadSafeTimeEntry>(te => te.Billable == dto.Billable));
        }

        [Fact, LogIfTooSlow]
        public async Task UpdatesTheTagIds()
        {
            var dto = prepareTest();

            await new UpdateTimeEntryInteractor(TimeService, DataSource, InteractorFactory, dto).Execute();

            await DataSource.Received().TimeEntries.Update(Arg.Is<IThreadSafeTimeEntry>(te => te.TagIds.SequenceEqual(dto.TagIds)));
        }

        [Fact, LogIfTooSlow]
        public async Task UpdatesTheWorkspaceId()
        {
            var dto = prepareTest();

            await new UpdateTimeEntryInteractor(TimeService, DataSource, InteractorFactory, dto).Execute();

            await DataSource.Received().TimeEntries.Update(Arg.Is<IThreadSafeTimeEntry>(te => te.WorkspaceId == dto.WorkspaceId));
        }

        [Fact, LogIfTooSlow]
        public async Task LeavesAllOtherPropertiesUnchanged()
        {
            var dto = prepareTest();

            await new UpdateTimeEntryInteractor(TimeService, DataSource, InteractorFactory, dto).Execute();

            await DataSource.Received().TimeEntries.Update(Arg.Is<IThreadSafeTimeEntry>(te => ensurePropertiesDidNotChange(te)));
        }

        [Fact, LogIfTooSlow]
        public async Task TriggersPushSync()
        {
            var syncManager = Substitute.For<ISyncManager>();
            DataSource.SyncManager.Returns(syncManager);
            var dto = prepareTest();

            await InteractorFactory.UpdateTimeEntry(dto).Execute();

            syncManager.Received().InitiatePushSync();
        }
    }
}
