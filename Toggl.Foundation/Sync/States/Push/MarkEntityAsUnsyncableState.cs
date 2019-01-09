using System;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.Sync.States.Push
{
    internal sealed class MarkEntityAsUnsyncableState<T> : ISyncState<(Exception Reason, T Entity)>
        where T : IThreadSafeModel
    {
        private readonly IBaseDataSource<T> dataSource;

        private readonly Func<T, string, T> createUnsyncableFrom;

        public StateResult<T> Done { get; } = new StateResult<T>();

        public MarkEntityAsUnsyncableState(
            IBaseDataSource<T> dataSource,
            Func<T, string, T> createUnsyncableFrom)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(createUnsyncableFrom, nameof(createUnsyncableFrom));

            this.dataSource = dataSource;
            this.createUnsyncableFrom = createUnsyncableFrom;
        }

        public IObservable<ITransition> Start((Exception Reason, T Entity) failedPush)
            => failedPush.Reason == null || failedPush.Entity == null
                ? failBecauseOfNullArguments(failedPush)
                : failedPush.Reason is ApiException apiException
                    ? markAsUnsyncable(failedPush.Entity, apiException.LocalizedApiErrorMessage)
                    : failBecauseOfUnexpectedError(failedPush.Reason);

        private IObservable<ITransition> failBecauseOfNullArguments((Exception Reason, T Entity) failedPush)
            => Observable.Throw<Transition<T>>(new ArgumentNullException(
                failedPush.Reason == null
                    ? nameof(failedPush.Reason)
                    : nameof(failedPush.Entity)));

        private IObservable<ITransition> failBecauseOfUnexpectedError(Exception reason)
            => Observable.Throw<Transition<T>>(reason);

        private IObservable<ITransition> markAsUnsyncable(T entity, string reason)
            => dataSource
                .OverwriteIfOriginalDidNotChange(entity, createUnsyncableFrom(entity, reason))
                .SelectMany(CommonFunctions.Identity)
                .OfType<UpdateResult<T>>()
                .Select(result => result.Entity)
                .DefaultIfEmpty(entity)
                .Select(unsyncable => Done.Transition(unsyncable));
    }
}
