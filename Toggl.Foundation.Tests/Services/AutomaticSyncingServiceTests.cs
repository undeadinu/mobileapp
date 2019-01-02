using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Login;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant;
using Xunit;
using static Toggl.Foundation.Services.AutomaticSyncingService;

namespace Toggl.Foundation.Tests.Services
{
    public sealed class AutomaticSyncingServiceTests
    {
        public abstract class BaseAutomaticSyncingServiceTest
        {
            protected IUserAccessManager UserAccessManager { get; } = Substitute.For<IUserAccessManager>();
            protected IAutomaticSyncingService AutomaticSyncingService { get; }
            protected ISubject<ITogglDataSource> LoggedIn { get; } = new Subject<ITogglDataSource>();
            protected ISubject<Unit> LoggedOut { get; } = new Subject<Unit>();
            protected ISubject<TimeSpan> AppResumedFromBackground { get; } = new Subject<TimeSpan>();
            protected ISyncManager SyncManager { get; } = Substitute.For<ISyncManager>();
            protected ITimeEntriesSource TimeEntriesSource { get; } = Substitute.For<ITimeEntriesSource>();
            protected ITogglDataSource DataSource { get; } = Substitute.For<ITogglDataSource>();
            protected IBackgroundService BackgroundService { get; } = Substitute.For<IBackgroundService>();
            protected IAnalyticsService AnalyticsService { get; } = Substitute.For<IAnalyticsService>();
            protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();

            protected BaseAutomaticSyncingServiceTest()
            {
                AutomaticSyncingService = new AutomaticSyncingService(BackgroundService, TimeService, AnalyticsService);
                BackgroundService.AppResumedFromBackground.Returns(AppResumedFromBackground);
                SyncManager.Errors.Returns(Observable.Never<Exception>());
                DataSource.SyncManager.Returns(SyncManager);
                DataSource.TimeEntries.Returns(TimeEntriesSource);
                UserAccessManager.UserLoggedIn.Returns(LoggedIn);
                UserAccessManager.UserLoggedOut.Returns(LoggedOut);
            }
        }

        public sealed class TheStartWithMethod : BaseAutomaticSyncingServiceTest
        {
            [Fact, LogIfTooSlow]
            public async Task SubscribesToResumingFromBackgroundSignal()
            {
                AutomaticSyncingService.SetupAutomaticSync(UserAccessManager);
                LoggedIn.OnNext(DataSource);
                AppResumedFromBackground.OnNext(MinimumTimeInBackgroundForFullSync);

                await SyncManager.Received().ForceFullSync();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksHowManyEntitiesChangeDuringAutomaticSyncWhenResumingFromBackground()
            {
                var syncedTimeEntry = new MockTimeEntry { SyncStatus = SyncStatus.InSync };
                var unsyncedTimeEntry = new MockTimeEntry { SyncStatus = SyncStatus.SyncNeeded };
                var createdSubject = new Subject<IThreadSafeTimeEntry>();
                var updatedSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
                var deletedSubject = new Subject<long>();
                var fullSyncSubject = new Subject<SyncState>();
                TimeEntriesSource.Created.Returns(createdSubject);
                TimeEntriesSource.Updated.Returns(updatedSubject);
                TimeEntriesSource.Deleted.Returns(deletedSubject);
                SyncManager.ForceFullSync().Returns(fullSyncSubject);
                SyncManager.Errors.Returns(Observable.Never<Exception>());

                AutomaticSyncingService.SetupAutomaticSync(UserAccessManager);
                LoggedIn.OnNext(DataSource);
                AppResumedFromBackground.OnNext(MinimumTimeInBackgroundForFullSync);
                createdSubject.OnNext(syncedTimeEntry);
                createdSubject.OnNext(unsyncedTimeEntry);
                updatedSubject.OnNext(new EntityUpdate<IThreadSafeTimeEntry>(123, syncedTimeEntry));
                updatedSubject.OnNext(new EntityUpdate<IThreadSafeTimeEntry>(123, unsyncedTimeEntry));
                deletedSubject.OnNext(123);
                fullSyncSubject.OnNext(SyncState.Sleep);
                fullSyncSubject.OnCompleted();

                bool wasCalled;
                do
                {
                    await Task.Delay(TimeSpan.FromSeconds(0.5));
                    wasCalled = AnalyticsService.NumberOfSyncedTimeEntriesWhenResumingTheAppFromBackground
                        .ReceivedCalls()
                        .Any();
                }
                while (!wasCalled);

                AnalyticsService.NumberOfSyncedTimeEntriesWhenResumingTheAppFromBackground.Received().Track(3);
            }

            [Fact, LogIfTooSlow]
            public async Task SubscribesToTheMidnightObservable()
            {
                var errors = new Subject<Exception>();
                SyncManager.Errors.Returns(errors);

                LoggedIn.OnNext(DataSource);
                errors.OnNext(new Exception());
                AppResumedFromBackground.OnNext(TimeSpan.FromHours(1));

                await SyncManager.DidNotReceive().ForceFullSync();
            }

            [Fact, LogIfTooSlow]
            public async Task StopsWhenSyncManagerFails()
            {
                var midnightSubject = new Subject<DateTimeOffset>();
                TimeService.MidnightObservable.Returns(midnightSubject);

                AutomaticSyncingService.SetupAutomaticSync(UserAccessManager);
                LoggedIn.OnNext(DataSource);
                midnightSubject.OnNext(new DateTimeOffset(2018, 12, 17, 00, 00, 00, TimeSpan.Zero));

                await SyncManager.Received().CleanUp();
            }
        }

        public sealed class TheStopMethod : BaseAutomaticSyncingServiceTest
        {
            [Fact, LogIfTooSlow]
            public async Task UnsubscribesFromTheSignalAfterLogout()
            {
                var subject = new Subject<TimeSpan>();
                BackgroundService.AppResumedFromBackground.Returns(subject.AsObservable());
                var errors = Observable.Never<Exception>();
                SyncManager.Errors.Returns(errors);

                AutomaticSyncingService.SetupAutomaticSync(UserAccessManager);
                LoggedIn.OnNext(DataSource);
                SyncManager.ClearReceivedCalls();
                LoggedOut.OnNext(Unit.Default);

                subject.OnNext(MinimumTimeInBackgroundForFullSync);

                await SyncManager.DidNotReceive().ForceFullSync();
            }

            [Fact, LogIfTooSlow]
            public void UnsubscribesFromTheBackgroundServiceObservableWhenExceptionIsCaught()
            {
                var subject = new Subject<TimeSpan>();
                var errorsSubject = new Subject<Exception>();
                BackgroundService.AppResumedFromBackground.Returns(subject.AsObservable());
                SyncManager.Errors.Returns(errorsSubject);

                AutomaticSyncingService.SetupAutomaticSync(UserAccessManager);
                LoggedIn.OnNext(DataSource);
                SyncManager.ClearReceivedCalls();
                errorsSubject.OnNext(new Exception());
                subject.OnNext(MinimumTimeInBackgroundForFullSync);

                SyncManager.DidNotReceive().ForceFullSync();
            }
        }
    }
}
