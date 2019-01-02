using Toggl.Foundation.Login;

namespace Toggl.Foundation.Services
{
    public interface IBackgroundSyncService
    {
        void SetupBackgroundSync(IUserAccessManager loginManager);
    }
}
