using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.Sync.States.Pull
{
    public interface IPersistState : ISyncState<IFetchObservables>
    {
        StateResult<IFetchObservables> Done { get; }
    }
}
