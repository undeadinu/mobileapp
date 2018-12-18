using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Toggl.Multivac.Extensions
{
    public sealed class RxActionNotEnabledException : Exception
    {
    }

    public class RxAction<TInput, TElement> : IDisposable
    {
        public IObservable<Exception> Errors { get; }
        public IObservable<TElement> Elements { get; }
        public IObservable<bool> Executing { get; }
        public IObservable<bool> Enabled { get; }
        public ISubject<TInput> Inputs { get; }

        private CompositeDisposable disposeBag { get; }

        private readonly IObservable<IObservable<TElement>> executionObservables;

        public RxAction(Func<TInput, IObservable<TElement>> workFactory, IScheduler mainScheduler, IObservable<bool> enabledIf = null)
        {
            if (enabledIf == null)
            {
                enabledIf = Observable.Return(true);
            }

            disposeBag = new CompositeDisposable();
            Inputs = new Subject<TInput>();

            var enabledSubject = new BehaviorSubject<bool>(false);
            Enabled = enabledSubject.AsObservable();

            var errorsSubject = new Subject<Exception>();
            Errors = errorsSubject.AsObservable();

            executionObservables = Inputs
                .WithLatestFrom(Enabled, (i, e) => (input: i, enabled: e))
                .SelectMany(tuple =>
                {
                    var enabled = tuple.enabled;
                    var input = tuple.input;

                    if (enabled)
                    {
                        var ob = workFactory(input)
                            .ObserveOn(mainScheduler)
                            .Do(CommonFunctions.DoNothing, error => errorsSubject.OnNext(error))
                            .Replay(1).RefCount();

                        return Observable.Return(ob);
                    }
                    else
                    {
                        errorsSubject.OnNext(new RxActionNotEnabledException());
                        return Observable.Empty<IObservable<TElement>>();
                    }
                })
                .Share();

            Elements = executionObservables
                .SelectMany(observable => observable.OnErrorResumeNext(Observable.Empty<TElement>()));

            Executing = executionObservables
                .SelectMany(exec =>
                {
                    var execution = exec
                        .SelectMany(_ => Observable.Empty<bool>())
                        .OnErrorResumeNext(Observable.Empty<bool>());

                    return Observable.Concat(Observable.Return(true), execution, Observable.Return(false));
                })
                .StartWith(false)
                .Replay(1).RefCount();

            Observable.CombineLatest(Executing, enabledIf, (executing, enabled) => !executing && enabled)
                .Subscribe(enabledSubject)
                .DisposedBy(disposeBag);
        }

        public IObservable<TElement> Execute(TInput value)
        {
            var subject = new ReplaySubject<TElement>();

            var error = Errors
                .Select(e => Observable.Throw<TElement>(e));

            executionObservables
                .Amb(error)
                .Take(1)
                .SelectMany(CommonFunctions.Identity)
                .Subscribe(subject)
                .DisposedBy(disposeBag);

            Inputs.OnNext(value);
            return subject.AsObservable();
        }

        public void Dispose()
        {
            disposeBag?.Dispose();
        }
    }
}
