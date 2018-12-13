using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Tests.Generators;
using Xunit;
using SyncOutcome = Toggl.Foundation.Models.SyncOutcome;
using SyncState = Toggl.Foundation.Sync.SyncState;

namespace Toggl.Foundation.Tests.Interactors.Workspace
{
    public sealed class RunBackgroundSyncInteractorTests
    {
        public sealed class TheConstructor : BaseInteractorTests
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useSyncManager, bool useAnalyticsService, bool userStopwatchProvider)
            {
                Action tryingToConstructWithNull = () => new RunBackgroundSyncInteractor(
                    useSyncManager ? SyncManager : null,
                    useAnalyticsService ? AnalyticsService : null,
                    userStopwatchProvider ? StopwatchProvider : null
                );

                tryingToConstructWithNull.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheExecuteMethod : BaseInteractorTests
        {
            private RunBackgroundSyncInteractor interactor;

            public TheExecuteMethod()
            {
                interactor = new RunBackgroundSyncInteractor(SyncManager, AnalyticsService, StopwatchProvider);
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFailedIfSyncFails()
            {
                SyncManager.ForceFullSync().Returns(Observable.Throw<SyncState>(new Exception()));
                (await interactor.Execute().SingleAsync()).Should().Be(SyncOutcome.Failed);
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsNewDataIfSyncSucceeds()
            {
                SyncManager.ForceFullSync().Returns(Observable.Return(SyncState.Sleep));
                (await interactor.Execute().SingleAsync()).Should().Be(SyncOutcome.NewData);
            }

            [Fact, LogIfTooSlow]
            public async Task TracksIfSyncSucceeds()
            {
                SyncManager.ForceFullSync().Returns(Observable.Return(SyncState.Sleep));
                await interactor.Execute().SingleAsync();
                AnalyticsService.BackgroundSyncStarted.Received().Track();
                AnalyticsService.BackgroundSyncFinished.Received().Track(nameof(SyncOutcome.NewData));
                AnalyticsService.BackgroundSyncFailed.DidNotReceive().Track(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            }

            [Fact, LogIfTooSlow]
            public async Task TracksIfSyncFails()
            {
                var exception = new Exception();
                SyncManager.ForceFullSync().Returns(Observable.Throw<SyncState>(exception));
                await interactor.Execute().SingleAsync();
                AnalyticsService.BackgroundSyncStarted.Received().Track();
                AnalyticsService.BackgroundSyncFinished.Received().Track(nameof(SyncOutcome.Failed));
                AnalyticsService.BackgroundSyncFailed.Received()
                    .Track(exception.GetType().FullName, exception.Message, exception.StackTrace);
            }
        }
    }
}
