using System;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Interactors
{
    internal class UpdateTimeEntryInteractor : IInteractor<IObservable<IThreadSafeTimeEntry>>
    {
        private readonly EditTimeEntryDto dto;
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IInteractorFactory interactorFactory;

        public UpdateTimeEntryInteractor(
            ITimeService timeService,
            ITogglDataSource dataSource,
            IInteractorFactory interactorFactory,
            EditTimeEntryDto dto)
        {
            Ensure.Argument.IsNotNull(dto, nameof(dto));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));

            this.dto = dto;
            this.dataSource = dataSource;
            this.timeService = timeService;
            this.interactorFactory = interactorFactory;
        }

        public IObservable<IThreadSafeTimeEntry> Execute()
            => interactorFactory.GetTimeEntryById(dto.Id)
                .Execute()
                .Select(createUpdatedTimeEntry)
                .SelectMany(dataSource.TimeEntries.Update)
                .Do(dataSource.SyncManager.InitiatePushSync);

        private TimeEntry createUpdatedTimeEntry(IThreadSafeTimeEntry timeEntry)
            => TimeEntry.Builder.Create(dto.Id)
                .SetDescription(dto.Description)
                .SetDuration(dto.StopTime.HasValue ? (long?)(dto.StopTime.Value - dto.StartTime).TotalSeconds : null)
                .SetTagIds(dto.TagIds)
                .SetStart(dto.StartTime)
                .SetTaskId(dto.TaskId)
                .SetBillable(dto.Billable)
                .SetProjectId(dto.ProjectId)
                .SetWorkspaceId(dto.WorkspaceId)
                .SetUserId(timeEntry.UserId)
                .SetIsDeleted(timeEntry.IsDeleted)
                .SetServerDeletedAt(timeEntry.ServerDeletedAt)
                .SetAt(timeService.CurrentDateTime)
                .SetSyncStatus(SyncStatus.SyncNeeded)
                .Build();
    }
}
