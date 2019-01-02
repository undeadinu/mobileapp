using System;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.Sync.States.Push.Interfaces
{
    public interface IPushEntityState<T> : ISyncState<T>
        where T : IThreadSafeModel
    {
        StateResult<ServerErrorException> ServerError { get; }

        StateResult<(Exception, T)> ClientError { get; }

        StateResult<Exception> UnknownError { get; }

        StateResult<TimeSpan> PreventOverloadingServer { get; }
    }
}
