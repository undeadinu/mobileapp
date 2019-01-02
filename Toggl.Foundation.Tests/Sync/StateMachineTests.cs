using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Xunit;

namespace Toggl.Foundation.Tests.Sync
{
    public sealed class StateMachineTests
    {
        public sealed class TheConstructor
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyArgumentIsNull(bool useHandler, bool useScheduler)
            {
                var handler = useHandler ? Substitute.For<ITransitionHandlerProvider>() : null;
                var scheduler = useScheduler ? Substitute.For<IScheduler>() : null;

                // ReSharper disable once ObjectCreationAsStatement
                Action ctor = () => new StateMachine(handler, scheduler);

                ctor.Should().Throw<ArgumentNullException>();
            }
        }

        public abstract class StateMachineTestBase
        {
            protected ITransitionHandlerProvider TransitionHandlers { get; } =
                Substitute.For<ITransitionHandlerProvider>();

            protected TestScheduler Scheduler { get; } = new TestScheduler();
            protected IStateMachine StateMachine { get; }
            protected List<StateMachineEvent> Events { get; } = new List<StateMachineEvent>();

            protected StateMachineTestBase()
            {
                SetTransitionHandler(Arg.Any<IStateResult>(), null);

                StateMachine = new StateMachine(TransitionHandlers, Scheduler);
                StateMachine.StateTransitions.Subscribe(e => Events.Add(e));
            }

            protected ITransition MakeTransitionSubstitute(TransitionHandler handler)
            {
                var transition = MakeTransitionSubstitute();
                SetTransitionHandler(transition.Result, handler);
                return transition;
            }

            protected ITransition MakeTransitionSubstitute()
            {
                var transition = Substitute.For<ITransition>();
                transition.Result.Returns(Substitute.For<IStateResult>());
                return transition;
            }

            protected void SetTransitionHandler(IStateResult result, TransitionHandler handler)
            {
                TransitionHandlers.GetTransitionHandler(result).Returns(handler);
            }

            protected StateMachineEvent Transition(ITransition transition) => new StateMachineTransition(transition);
            protected StateMachineEvent DeadEnd(ITransition transition) => new StateMachineDeadEnd(transition);
            protected StateMachineEvent Error => new StateMachineError(null);
        }

        public sealed class TheStartTransitionMethod : StateMachineTestBase
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfArgumentIsNull()
            {
                Action callWithNull = () => StateMachine.Start(null);

                callWithNull.Should().Throw<ArgumentNullException>();
            }

            [Fact, LogIfTooSlow]
            public void AsksTransitionHandlerProviderForHandler()
            {
                var transition = MakeTransitionSubstitute();

                StateMachine.Start(transition);

                TransitionHandlers.Received().GetTransitionHandler(transition.Result);
            }

            [Fact, LogIfTooSlow]
            public void ReportsDeadEndIfThereIsNoHandler()
            {
                var transition = MakeTransitionSubstitute();

                StateMachine.Start(transition);

                Events.ShouldBeSameEventsAs(
                    DeadEnd(transition)
                );
            }

            [Fact, LogIfTooSlow]
            public void ReportsDeadEndIfThereIsNoValidHandler()
            {
                MakeTransitionSubstitute(_ => Observable.Never<ITransition>());
                var transition = Substitute.For<ITransition>();

                StateMachine.Start(transition);

                Events.ShouldBeSameEventsAs(
                    DeadEnd(transition)
                );
            }

            [Fact, LogIfTooSlow]
            public void PerformsTransitionIfThereIsAHandler()
            {
                var transition = MakeTransitionSubstitute(_ => Observable.Never<ITransition>());

                StateMachine.Start(transition);

                Events.ShouldBeSameEventsAs(
                    Transition(transition)
                );
            }

            [Fact, LogIfTooSlow]
            public void ThrowsIfMachineIsAlreadyRunning()
            {
                SetTransitionHandler(Arg.Any<IStateResult>(), _ => Observable.Never<ITransition>());
                StateMachine.Start(MakeTransitionSubstitute());

                Action callSecondTime = () => StateMachine.Start(MakeTransitionSubstitute());

                callSecondTime.Should().Throw<InvalidOperationException>();
            }
        }

        public sealed class TheStateTransitionsObservable : StateMachineTestBase
        {
            [Fact, LogIfTooSlow]
            public void ReportsErrorIfTheStateErrors()
            {
                var transition = MakeTransitionSubstitute(_ => Observable.Throw<ITransition>(new Exception()));

                StateMachine.Start(transition);

                Events.ShouldBeSameEventsAs(
                    Transition(transition),
                    Error
                );
            }

            [Fact, LogIfTooSlow]
            public void ReportsErrorIfTheStateCompletesWithoutTransition()
            {
                var transition = MakeTransitionSubstitute(_ => Observable.Empty<ITransition>());

                StateMachine.Start(transition);

                Events.ShouldBeSameEventsAs(
                    Transition(transition),
                    Error
                );
            }

            [Fact, LogIfTooSlow]
            public void ReportsErrorIfTheStateCompletesWithMoreThanOneTransition()
            {
                var transition = MakeTransitionSubstitute(
                    _ => new[] { MakeTransitionSubstitute(), MakeTransitionSubstitute() }.ToObservable()
                    );

                StateMachine.Start(transition);

                Events.ShouldBeSameEventsAs(
                    Transition(transition),
                    Error
                );
            }

            [Fact(Skip="there is currently no timeout"), LogIfTooSlow]
            public void ReportsTransitionIfStateTakesLessThanOneMinute()
            {
                var stateSubject = new Subject<ITransition>();
                var transition = MakeTransitionSubstitute(_ => stateSubject.AsObservable());
                var transition2 = MakeTransitionSubstitute(_ => Observable.Never<ITransition>());

                StateMachine.Start(transition);
                Scheduler.AdvanceBy(TimeSpan.FromSeconds(55).Ticks);
                stateSubject.OnNext(transition2);
                stateSubject.OnCompleted();

                Events.ShouldBeSameEventsAs(
                    Transition(transition),
                    Transition(transition2)
                );
            }

            [Fact, LogIfTooSlow]
            public void DoesNotReportErrorIfMultipleStatesTogetherTakeLongerThanOneMinute()
            {
                var stateSubject = new Subject<ITransition>();
                var stateSubject2 = new Subject<ITransition>();
                var transition = MakeTransitionSubstitute(_ => stateSubject.AsObservable());
                var transition2 = MakeTransitionSubstitute(_ => stateSubject2.AsObservable());
                var transition3 = MakeTransitionSubstitute(_ => Observable.Never<ITransition>());

                StateMachine.Start(transition);
                Scheduler.AdvanceBy(TimeSpan.FromSeconds(40).Ticks);
                stateSubject.OnNext(transition2);
                stateSubject.OnCompleted();
                Scheduler.AdvanceBy(TimeSpan.FromSeconds(40).Ticks);
                stateSubject2.OnNext(transition3);
                stateSubject2.OnCompleted();
                Scheduler.AdvanceBy(TimeSpan.FromSeconds(40).Ticks);

                Events.ShouldBeSameEventsAs(
                    Transition(transition),
                    Transition(transition2),
                    Transition(transition3)
                );
            }

            [Fact(Skip="there is currently no timeout"), LogIfTooSlow]
            public void ReportsErrorIfStateTakesMoreThanOneMinute()
            {
                var stateSubject = new Subject<ITransition>();
                var transition = MakeTransitionSubstitute(_ => stateSubject.AsObservable());
                var transition2 = MakeTransitionSubstitute(_ => Observable.Never<ITransition>());

                StateMachine.Start(transition);
                Scheduler.AdvanceBy(TimeSpan.FromSeconds(65).Ticks);
                stateSubject.OnNext(transition2);
                stateSubject.OnCompleted();

                Events.ShouldBeSameEventsAs(
                    Transition(transition),
                    Error
                );
            }

            [Fact, LogIfTooSlow]
            public void GetsNewStateObservableEveryTimeStateIsEntered()
            {
                var transition = MakeTransitionSubstitute();
                var deadEnd = MakeTransitionSubstitute();
                var observablesGotten = 0;
                SetTransitionHandler(transition.Result, _ =>
                {
                    observablesGotten++;
                    return Observable.Return(observablesGotten == 3 ? deadEnd : transition);
                });

                StateMachine.Start(transition);

                Events.ShouldBeSameEventsAs(
                    Transition(transition),
                    Transition(transition),
                    Transition(transition),
                    DeadEnd(deadEnd)
                );
            }
        }

        public sealed class TheFreezeMethod : StateMachineTestBase
        {
            [Fact, LogIfTooSlow]
            public void ReachesDeadEndAfterTheStateMachineIsFrozen()
            {
                var stateSubject = new Subject<ITransition>();
                var stateSubject2 = new Subject<ITransition>();
                var transition = MakeTransitionSubstitute(_ => stateSubject.AsObservable());
                var transition2 = MakeTransitionSubstitute(_ => stateSubject2.AsObservable());
                var transition3 = MakeTransitionSubstitute(_ => Observable.Never<ITransition>());

                StateMachine.Start(transition);
                stateSubject.OnNext(transition2);
                stateSubject.OnCompleted();
                StateMachine.Freeze();
                stateSubject2.OnNext(transition3);
                stateSubject2.OnCompleted();

                Events.ShouldBeSameEventsAs(
                    Transition(transition),
                    Transition(transition2),
                    DeadEnd(transition3)
                );
            }

            [Fact, LogIfTooSlow]
            public void DoesNotPeformTransitionsAfterFreezing()
            {
                var stateSubject = new Subject<ITransition>();
                var stateSubject2 = new Subject<ITransition>();
                var transition3Performed = false;
                var transition = MakeTransitionSubstitute(_ => stateSubject.AsObservable());
                var transition2 = MakeTransitionSubstitute(_ => stateSubject2.AsObservable());
                var transition3 = MakeTransitionSubstitute(_ =>
                    {
                        transition3Performed = true;
                        return Observable.Never<ITransition>();
                    });

                StateMachine.Start(transition);
                stateSubject.OnNext(transition2);
                stateSubject.OnCompleted();
                StateMachine.Freeze();
                stateSubject2.OnNext(transition3);
                stateSubject2.OnCompleted();

                transition3Performed.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void ThrowsAnExceptionWhenTheStateMachineIsStartedAfterBeingFrozen()
            {
                StateMachine.Freeze();

                Action starting = () => StateMachine.Start(Substitute.For<ITransition>());

                starting.Should().Throw<InvalidOperationException>();
            }
        }
    }

    internal static class StateMachineTestExtensions
    {
        public static void ShouldBeSameEventsAs(this List<StateMachineEvent> actualEvents,
            params StateMachineEvent[] expectedEvents)
        {
            Ensure.Argument.IsNotNull(expectedEvents, nameof(expectedEvents));

            actualEvents.Should().HaveCount(expectedEvents.Length);

            for (var i = 0; i < expectedEvents.Length; i++)
            {
                var actual = actualEvents[i];
                var expected = expectedEvents[i];

                try
                {
                    actual.Should().BeOfType(expected.GetType());

                    switch (expected)
                    {
                        case StateMachineTransition transition:
                            ((StateMachineTransition)actual).Transition.Should().Be(transition.Transition);
                            break;
                        case StateMachineDeadEnd deadEnd:
                            ((StateMachineDeadEnd)actual).Transition.Should().Be(deadEnd.Transition);
                            break;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Found unexpected event at index {i}.", e);
                }
            }
        }
    }

}
