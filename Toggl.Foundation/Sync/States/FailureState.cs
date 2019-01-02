using System;
using System.Reactive.Linq;

namespace Toggl.Foundation.Sync.States
{
    internal sealed class FailureState : ISyncState<Exception>
    {
        public IObservable<ITransition> Start(Exception exception)
            => Observable.Throw<ITransition>(exception);
    }
}
