using System;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Sync.States;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;
using Xunit;
using static Toggl.Foundation.Sync.SyncState;

namespace Toggl.Foundation.Tests.Sync
{
    public sealed class TheDeadlocks
    {
        public abstract class BaseDeadlockTests
        {
            protected ISyncManager SyncManager;
            protected TransitionHandlerProvider Transitions;
            protected IScheduler Scheduler;
            protected IStateMachine StateMachine;
            protected ISyncStateQueue Queue;
            protected IStateMachineOrchestrator Orchestrator;
            protected StateMachineEntryPoints EntryPoints;

            protected BaseDeadlockTests(IScheduler scheduler)
            {
                Scheduler = scheduler;
                Reset();
            }

            protected void Reset()
            {
                var analyticsService = Substitute.For<IAnalyticsService>();
                var lastTimeUsageStorage = Substitute.For<ILastTimeUsageStorage>();
                var timeService = Substitute.For<ITimeService>();
                Queue = new SyncStateQueue();
                Transitions = new TransitionHandlerProvider();
                Scheduler = new TestScheduler();
                StateMachine = new StateMachine(Transitions, Scheduler);
                EntryPoints = new StateMachineEntryPoints();
                Orchestrator = new StateMachineOrchestrator(StateMachine, EntryPoints);
                SyncManager = new SyncManager(Queue, Orchestrator, analyticsService, lastTimeUsageStorage, timeService);
            }

            protected StateResult PreparePullTransitions(int n)
                => PrepareTransitions(EntryPoints.StartPullSync, n);

            protected StateResult PreparePushTransitions(int n)
                => PrepareTransitions(EntryPoints.StartPushSync, n);

            protected StateResult PrepareTransitions(StateResult entryPoint, int n)
            {
                var lastResult = entryPoint;
                for (byte i = 0; i < n; i++)
                {
                    var nextResult = new StateResult();
                    Func<IObservable<ITransition>> transition = () => Observable.Create<ITransition>(async observer =>
                    {
                        await Task.Delay(1).ConfigureAwait(false);
                        observer.OnNext(new Transition(nextResult));
                        observer.OnCompleted();
                    });

                    Transitions.ConfigureTransition(lastResult, new TestSyncState(transition));

                    lastResult = nextResult;
                }

                return lastResult;
            }

            protected void PrepareFailingTransition(StateResult lastResult)
            {
                Func<IObservable<ITransition>> failingTransition = () => Observable.Throw<ITransition>(new TestException());
                Transitions.ConfigureTransition(lastResult, new TestSyncState(failingTransition));
            }
        }

        public sealed class TheSyncManager : BaseDeadlockTests
        {
            public TheSyncManager() : base(Substitute.For<IScheduler>())
            {
            }

            [Fact, LogIfTooSlow]
            public void DoesNotGetStuckInADeadlockWhenThereAreNoTransitionHandlersForFullSync()
            {
                var isLocked = isLockedAfterFullSync();

                isLocked.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotGetStuckInADeadlockWhenThereAreNoTransitionHandlersForPushSync()
            {
                var isLocked = isLockedAfterFullSync();

                isLocked.Should().BeFalse();
            }

            [Theory]
            [InlineData(3)]
            [InlineData(5)]
            [InlineData(10)]
            public void DoesNotGetStuckInADeadlockWhenThereAreSomeTransitionHandlersForFullSync(int n)
            {
                Reset();
                PreparePullTransitions(n);
                PreparePushTransitions(n);

                var isLocked = isLockedAfterFullSync();

                isLocked.Should().BeFalse();
            }

            [Theory]
            [InlineData(3)]
            [InlineData(5)]
            [InlineData(10)]
            public void DoesNotGetStuckInADeadlockWhenThereAreSomeTransitionHandlersForPushSync(int n)
            {
                Reset();
                PreparePushTransitions(n);

                var isLocked = isLockedAfterPushSync();

                isLocked.Should().BeFalse();
            }

            [Theory]
            [InlineData(3)]
            [InlineData(5)]
            [InlineData(10)]
            public void DoesNotGetStuckInADeadlockWhenSomeTransitionFailsForFullSync(int n)
            {
                Reset();
                var lastResult = PreparePullTransitions(n);
                PrepareFailingTransition(lastResult);

                var isLocked = isLockedAfterFullSync();

                isLocked.Should().BeFalse();
            }

            [Theory]
            [InlineData(3)]
            [InlineData(5)]
            [InlineData(10)]
            public void DoesNotGetStuckInADeadlockWhenSomeTransitionFailsForPushSync(int n)
            {
                Reset();
                var lastResult = PreparePushTransitions(n);
                PrepareFailingTransition(lastResult);

                var isLocked = isLockedAfterPushSync();

                isLocked.Should().BeFalse();
            }

            private bool isLockedAfterFullSync()
                => isLockedAfterSync(SyncManager.ForceFullSync);

            private bool isLockedAfterPushSync()
                => isLockedAfterSync(SyncManager.PushSync);

            private bool isLockedAfterSync(Func<IObservable<SyncState>> sync)
            {
                var stateObservable = sync();
                stateObservable.SkipWhile(state => state != Sleep).Wait();
                // now that the sync finished the sync manager must have the IsRunningSync
                // set to false, otherwise no further synchronizations are possible and the
                // internal state is inconsistent and can't be fixed
                return SyncManager.IsRunningSync;
            }
        }

        public sealed class TheStateMachine : BaseDeadlockTests
        {
            public TheStateMachine() : base(new TestScheduler())
            {
            }

            [Fact, LogIfTooSlow]
            public void DoesNotGetStuckInADeadlockWhenThereIsNoTransitionHandler()
            {
                var someResult = new StateResult();
                var differentResult = new StateResult();

                var secondStart = startStateMachineAndPrepareSecondStart(someResult, differentResult);

                secondStart.Should().NotThrow<InvalidOperationException>();
            }

            [Theory]
            [InlineData(3)]
            [InlineData(5)]
            [InlineData(10)]
            public void DoesNotGetStuckInADeadlockWhenThereAreSomeTransitionHandlers(int n)
            {
                Reset();
                var someResult = new StateResult();
                PrepareTransitions(someResult, n);

                var secondStart = startStateMachineAndPrepareSecondStart(someResult, someResult);

                secondStart.Should().NotThrow<InvalidOperationException>();
            }

            [Theory]
            [InlineData(3)]
            [InlineData(5)]
            [InlineData(10)]
            public void DoesNotGetStuckInADeadlockWhenATransitionHandlerFails(int n)
            {
                Reset();
                var someResult = new StateResult();
                var lastResult = PrepareTransitions(someResult, n);
                PrepareFailingTransition(lastResult);

                var secondStart = startStateMachineAndPrepareSecondStart(someResult, someResult);

                secondStart.Should().NotThrow<InvalidOperationException>();
            }

            [Property(Skip = "there is currently no timeout")]
            public void DoesNotGetStuckInADeadlockWhenSomeTransitionTimeOuts(int n)
            {
                Reset();
                var someResult = new StateResult();
                var lastResult = PrepareTransitions(someResult, n);
                Transitions.ConfigureTransition(lastResult, new TestSyncState(Observable.Never<ITransition>));

                var observable = stateMachineFinised();
                StateMachine.Start(someResult.Transition());
                ((TestScheduler)Scheduler).AdvanceBy(TimeSpan.FromSeconds(62).Ticks);
                observable.Wait();
                Action secondStart = () => StateMachine.Start(someResult.Transition());

                secondStart.Should().NotThrow<InvalidOperationException>();
            }

            private Action startStateMachineAndPrepareSecondStart(StateResult first, StateResult second)
            {
                var observable = stateMachineFinised();
                StateMachine.Start(first.Transition());
                observable.Wait();
                return () => StateMachine.Start(second.Transition());
            }

            private IObservable<StateMachineEvent> stateMachineFinised()
                => StateMachine.StateTransitions
                    .ConnectedReplay()
                    .SkipWhile(isNotFinished)
                    .FirstAsync();

            private bool isNotFinished(StateMachineEvent next)
                => !(next is StateMachineDeadEnd || next is StateMachineError);
        }
    }
}
