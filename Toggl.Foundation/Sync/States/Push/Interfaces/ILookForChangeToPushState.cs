using System;

namespace Toggl.Foundation.Sync.States.Push.Interfaces
{
    public interface ILookForChangeToPushState<T> : ISyncState
    {
        StateResult<T> ChangeFound { get; }

        StateResult NoMoreChanges { get; }
    }
}
