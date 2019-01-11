using System;
using System.Collections.Generic;
using System.Reactive;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Interactors.Generic;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Suggestions;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Interactors
{
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        public IInteractor<IObservable<IThreadSafeTimeEntry>> CreateTimeEntry(ITimeEntryPrototype prototype)
            => new CreateTimeEntryInteractor(
                idProvider,
                timeService,
                dataSource,
                analyticsService,
                intentDonationService,
                prototype,
                prototype.StartTime,
                prototype.Duration);

        public IInteractor<IObservable<IThreadSafeTimeEntry>> ContinueTimeEntry(ITimeEntryPrototype prototype)
            => new CreateTimeEntryInteractor(
                idProvider,
                timeService,
                dataSource,
                analyticsService,
                intentDonationService,
                prototype,
                timeService.CurrentDateTime,
                null,
                TimeEntryStartOrigin.Continue);

        public IInteractor<IObservable<IThreadSafeTimeEntry>> StartSuggestion(Suggestion suggestion)
            => new CreateTimeEntryInteractor(
                idProvider,
                timeService,
                dataSource,
                analyticsService,
                intentDonationService,
                suggestion,
                timeService.CurrentDateTime,
                null,
            TimeEntryStartOrigin.Suggestion);

        public IInteractor<IObservable<IThreadSafeTimeEntry>> ContinueMostRecentTimeEntry()
            => new ContinueMostRecentTimeEntryInteractor(
                idProvider,
                timeService,
                dataSource,
                analyticsService);

        public IInteractor<IObservable<Unit>> DeleteTimeEntry(long id)
            => new DeleteTimeEntryInteractor(timeService, dataSource.TimeEntries, this, id);

        public IInteractor<IObservable<IThreadSafeTimeEntry>> GetTimeEntryById(long id)
            => new GetByIdInteractor<IThreadSafeTimeEntry, IDatabaseTimeEntry>(dataSource.TimeEntries, id);

        public IInteractor<IObservable<IEnumerable<IThreadSafeTimeEntry>>> GetAllTimeEntriesVisibleToTheUser()
            => new GetAllTimeEntriesVisibleToTheUserInteractor(dataSource.TimeEntries);

        public IInteractor<IObservable<IThreadSafeTimeEntry>> UpdateTimeEntry(EditTimeEntryDto dto)
            => new UpdateTimeEntryInteractor(timeService, dataSource, this, dto);

        public IInteractor<IObservable<IThreadSafeTimeEntry>> StopTimeEntry(DateTimeOffset currentDateTime, TimeEntryStopOrigin origin)
            => new StopTimeEntryInteractor(timeService, dataSource.TimeEntries, currentDateTime, analyticsService, origin);

        public IInteractor<IObservable<Unit>> ObserveTimeEntriesChanges()
            => new ObserveTimeEntriesChangesInteractor(dataSource);
    }
}
