using System;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Sync;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Extensions
{
    public static class ObservableExtensions
    {
        public static IObservable<T2> ThenExecute<T1, T2>(this IObservable<T1> observable, Func<IObservable<T2>> continuation)
            => observable.SelectMany(_ => continuation());

        public static IObservable<T> Track<T>(this IObservable<T> observable, ITrackableEvent trackableEvent, IAnalyticsService service)
            => observable.Do(_ => service.Track(trackableEvent));

        public static IObservable<T> Track<T>(this IObservable<T> observable, Func<T, ITrackableEvent> eventFactory, IAnalyticsService service)
            => observable.Do(value => service.Track(eventFactory(value)));

        public static IObservable<T> Track<T>(this IObservable<T> observable, IAnalyticsEvent analyticsEvent)
            => observable.Do(analyticsEvent.Track);

        public static IObservable<T> Track<T>(this IObservable<T> observable, IAnalyticsEvent<T> analyticsEvent)
            => observable.Do(analyticsEvent.Track);

        public static IObservable<T> Track<T, T1>(this IObservable<T> observable, IAnalyticsEvent<T1> analyticsEvent, T1 parameter)
            => observable.Do(_ => analyticsEvent.Track(parameter));

        public static IObservable<T> Track<T, T1, T2>(this IObservable<T> observable, IAnalyticsEvent<T1, T2> analyticsEvent, T1 param1, T2 param2)
            => observable.Do(_ => analyticsEvent.Track(param1, param2));

        public static IObservable<T> Track<T, T1, TException>(this IObservable<T> observable, IAnalyticsEvent<T1> analyticsEvent, T1 parameter)
            where TException : Exception
            => observable.Catch<T, TException>(exception =>
            {
                analyticsEvent.Track(parameter);
                return Observable.Throw<T>(exception);
            });

        public static IObservable<ITransition> OnErrorReturnResult<T>(this IObservable<ITransition> observable, StateResult<T> errorResult)
            where T : Exception
            => observable.Catch((T exception) => Observable.Return(errorResult.Transition(exception)));
    }
}
