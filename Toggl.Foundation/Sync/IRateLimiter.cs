using System;
using System.Reactive;

namespace Toggl.Foundation.Sync
{
    public interface IRateLimiter
    {
        IObservable<Unit> WaitForFreeSlot();
    }
}
