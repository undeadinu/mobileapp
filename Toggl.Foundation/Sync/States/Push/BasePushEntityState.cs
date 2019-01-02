using System;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync.States.Push.Interfaces;
using Toggl.Multivac;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.Sync.States.Push
{
    public abstract class BasePushEntityState<T> : IPushEntityState<T>
        where T : class, IThreadSafeModel
    {
        protected IAnalyticsService AnalyticsService { get; }

        public StateResult<ServerErrorException> ServerError { get; } = new StateResult<ServerErrorException>();

        public StateResult<(Exception, T)> ClientError { get; } = new StateResult<(Exception, T)>();

        public StateResult<Exception> UnknownError { get; } = new StateResult<Exception>();

        public StateResult<TimeSpan> PreventOverloadingServer { get; } = new StateResult<TimeSpan>();

        protected BasePushEntityState(IAnalyticsService analyticsService)
        {
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));

            AnalyticsService = analyticsService;
        }

        protected Func<Exception, IObservable<ITransition>> Fail(T entity, PushSyncOperation operation)
            => exception
                => Observable
                    .Return(entity)
                    .Track(typeof(T).ToSyncErrorAnalyticsEvent(AnalyticsService), $"{operation}:{exception.Message}")
                    .Track(AnalyticsService.EntitySyncStatus, entity.GetSafeTypeName(), $"{operation}:{Resources.Failure}")
                    .SelectMany(_ => shouldRethrow(exception)
                        ? Observable.Throw<ITransition>(exception)
                        : Observable.Return(failTransition(entity, exception)));

        private bool shouldRethrow(Exception e)
            => e is ApiDeprecatedException || e is ClientDeprecatedException || e is UnauthorizedException || e is OfflineException;

        private ITransition failTransition(T entity, Exception e)
            => e is ServerErrorException serverError
                ? ServerError.Transition(serverError)
                : e is ClientErrorException
                    ? ClientError.Transition((e, entity))
                    : (ITransition)UnknownError.Transition(e);

        public abstract IObservable<ITransition> Start(T entity);
    }
}
