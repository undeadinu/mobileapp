using Toggl.Foundation.DataSources;
using Toggl.Foundation.Login;
using Toggl.Foundation.Sync;

namespace Toggl.Foundation.Services
{
    public interface IAutomaticSyncingService
    {
        void SetupAutomaticSync(IUserAccessManager userAccessManager);
    }
}
