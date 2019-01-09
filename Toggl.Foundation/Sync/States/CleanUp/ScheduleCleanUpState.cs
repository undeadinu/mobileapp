using System;
using System.Reactive.Linq;
using Toggl.Multivac;

namespace Toggl.Foundation.Sync.States.CleanUp
{
    internal sealed class ScheduleCleanUpState : ISyncState<IFetchObservables>
    {
        private readonly ISyncStateQueue queue;

        public StateResult<IFetchObservables> Done { get; } = new StateResult<IFetchObservables>();

        public ScheduleCleanUpState(ISyncStateQueue queue)
        {
            Ensure.Argument.IsNotNull(queue, nameof(queue));
            this.queue = queue;
        }

        public IObservable<ITransition> Start(IFetchObservables fetch)
        {
            queue.QueueCleanUp();
            return Observable.Return(Done.Transition(fetch));
        }
    }
}
