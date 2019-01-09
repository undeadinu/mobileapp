using Toggl.Foundation.Sync;

namespace Toggl.Foundation.Services
{
    public interface ISyncErrorHandlingService
    {
        void HandleErrorsOf(ISyncManager syncManager);
    }
}
