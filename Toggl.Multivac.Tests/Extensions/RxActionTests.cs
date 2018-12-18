using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;
using Toggl.Multivac.Extensions;
using Xunit;

namespace Toggl.Multivac.Tests
{
    public sealed class RxActionTests
    {
        public sealed class TheErrorsProperty : ReactiveTest
        {
            [Fact, LogIfTooSlow]
            public void ForwardsErrorsInTheExecution()
            {
                var testScheduler = new TestScheduler();
                var exception = new Exception("This is an error");

                var observable = testScheduler.CreateColdObservable(
                    OnError<string>(10, exception)
                );

                var action = new RxAction<Unit, string>(_ => observable, testScheduler);

                var observer = testScheduler.CreateObserver<Exception>();

                action.Errors.Subscribe(observer);

                testScheduler.Schedule(TimeSpan.FromTicks(300), () => action.Execute(Unit.Default));
                testScheduler.Start();

                observer.Messages.AssertEqual(
                    OnNext(311, exception)
                );
            }
        }

        public sealed class TheExecutingProperty : ReactiveTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsTrueWhileExecuting()
            {
                var testScheduler = new TestScheduler();
                var observer = testScheduler.CreateObserver<bool>();
                var observable = testScheduler.CreateColdObservable(
                    OnNext(10, "0"),
                    OnNext(20, "1"),
                    OnCompleted<string>(30)
                );

                var action = new RxAction<Unit, string>(_ => observable, testScheduler);

                action.Executing.Subscribe(observer);

                testScheduler.Schedule(TimeSpan.FromTicks(300), () =>
                {
                    action.Execute(Unit.Default);
                });
                testScheduler.Start();

                observer.Messages.AssertEqual(
                    OnNext(0, false),
                    OnNext(300, true),
                    OnNext(331, false)
                );
            }
        }

        public sealed class TheEnabledProperty : ReactiveTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsFalseWhileExecuting()
            {
                var testScheduler = new TestScheduler();
                var observer = testScheduler.CreateObserver<bool>();
                var observable = testScheduler.CreateColdObservable(
                    OnNext(10, "1"),
                    OnNext(20, "2"),
                    OnCompleted<string>(30)
                );

                var action = new RxAction<Unit, string>(_ => observable, testScheduler);

                action.Enabled.Subscribe(observer);

                testScheduler.Schedule(TimeSpan.FromTicks(300), () =>
                {
                    action.Execute(Unit.Default);
                });
                testScheduler.Start();

                observer.Messages.AssertEqual(
                    OnNext(0, true),
                    OnNext(300, false),
                    OnNext(331, true)
                );
            }
        }

        public sealed class TheExecuteMethod : ReactiveTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsTheResultsOfTheOperation()
            {
                var testScheduler = new TestScheduler();
                var observer = testScheduler.CreateObserver<string>();
                var observable = testScheduler.CreateColdObservable(
                    OnNext(10, "0"),
                    OnNext(20, "1"),
                    OnCompleted<string>(30)
                );

                var action = new RxAction<Unit, string>(_ => observable, testScheduler);

                testScheduler.Schedule(TimeSpan.FromTicks(300), () => action.Execute(Unit.Default).Subscribe(observer));
                testScheduler.Start();

                observer.Messages.AssertEqual(
                    OnNext(311, "0"),
                    OnNext(321, "1"),
                    OnCompleted<string>(331)
                );
            }
        }
    }
}
