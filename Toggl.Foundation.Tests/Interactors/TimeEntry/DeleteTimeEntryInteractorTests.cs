using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Exceptions;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors
{
    public sealed class DeleteTimeEntryInteractorTests : BaseInteractorTests
    {
        private readonly MockTimeEntry timeEntry;
        private readonly DeleteTimeEntryInteractor interactor;
        private static DateTimeOffset now = new DateTimeOffset(2018, 05, 14, 18, 00, 00, TimeSpan.Zero);

        public DeleteTimeEntryInteractorTests()
        {
            timeEntry = new MockTimeEntry
            {
                Id = 12
            };

            InteractorFactory.GetTimeEntryById(timeEntry.Id)
                .Execute()
                .Returns(Observable.Return(timeEntry));
            DataSource.TimeEntries.Update(Arg.Any<IThreadSafeTimeEntry>())
                .Returns(callInfo => Observable.Return(callInfo.Arg<IThreadSafeTimeEntry>()));

            interactor = new DeleteTimeEntryInteractor(TimeService, DataSource.TimeEntries, InteractorFactory, timeEntry.Id);
        }

        [Fact, LogIfTooSlow]
        public async Task SetsTheDeletedFlag()
        {
            await interactor.Execute().LastOrDefaultAsync();

            await DataSource.Received().TimeEntries.Update(Arg.Is<IThreadSafeTimeEntry>(te => te.IsDeleted == true));
        }

        [Fact, LogIfTooSlow]
        public async Task SetsTheSyncNeededStatus()
        {
            await interactor.Execute().LastOrDefaultAsync();

            await DataSource.Received().TimeEntries.Update(Arg.Is<IThreadSafeTimeEntry>(te => te.SyncStatus == SyncStatus.SyncNeeded));
        }

        [Fact, LogIfTooSlow]
        public async Task SetsTheAtDateToCurrentTime()
        {
            var newNow = new DateTimeOffset(2018, 09, 01, 11, 22, 33, TimeSpan.Zero);
            TimeService.CurrentDateTime.Returns(newNow);

            await interactor.Execute().LastOrDefaultAsync();

            await DataSource.Received().TimeEntries.Update(Arg.Is<IThreadSafeTimeEntry>(te => te.At == newNow));
        }

        [Fact, LogIfTooSlow]
        public async Task UpdatesTheCorrectTimeEntry()
        {
            await interactor.Execute().LastOrDefaultAsync();

            await DataSource.Received().TimeEntries.Update(Arg.Is<IThreadSafeTimeEntry>(te => te.Id == timeEntry.Id));
        }

        [Fact, LogIfTooSlow]
        public void EmitsSingleElementBeforeCompleting()
        {
            var observer = Substitute.For<IObserver<Unit>>();

            interactor.Execute().Subscribe(observer);

            observer.Received(1).OnNext(Arg.Any<Unit>());
            observer.Received(1).OnCompleted();
        }

        [Fact, LogIfTooSlow]
        public void PropagatesErrorIfUpdateFails()
        {
            var failingTimeEntry = Models.TimeEntry.Builder.Create(12)
                  .SetStart(now)
                  .SetSyncStatus(SyncStatus.InSync)
                  .SetDescription("")
                  .SetUserId(11)
                  .SetWorkspaceId(10)
                  .SetAt(now)
                  .Build();

            var timeEntryObservable = Observable.Return(failingTimeEntry);
            var errorObservable = Observable.Throw<IThreadSafeTimeEntry>(new DatabaseOperationException<IDatabaseTimeEntry>(new Exception()));
            InteractorFactory.GetTimeEntryById(Arg.Is(timeEntry.Id))
                .Execute()
                .Returns(timeEntryObservable);
            DataSource.TimeEntries.Update(Arg.Any<IThreadSafeTimeEntry>()).Returns(errorObservable);
            var observer = Substitute.For<IObserver<Unit>>();

            interactor.Execute().Subscribe(observer);

            observer.Received().OnError(Arg.Any<DatabaseOperationException<IDatabaseTimeEntry>>());
        }

        [Fact, LogIfTooSlow]
        public void PropagatesErrorIfTimeEntryIsNotInRepository()
        {
            var otherTimeEntry = Substitute.For<IThreadSafeTimeEntry>();
            otherTimeEntry.Id.Returns(12);
            var observer = Substitute.For<IObserver<Unit>>();
            DataSource.TimeEntries.Update(Arg.Any<IThreadSafeTimeEntry>())
                .Returns(Observable.Throw<IThreadSafeTimeEntry>(new DatabaseOperationException<IDatabaseTimeEntry>(new Exception())));

            interactor.Execute().Subscribe(observer);

            observer.Received().OnError(Arg.Any<DatabaseOperationException<IDatabaseTimeEntry>>());
        }
    }
}
