using Toggl.Foundation.Services;
using UIKit;

namespace Toggl.Daneel.Services
{
    public sealed class BackgroundSyncServiceIos : BaseBackgroundSyncService
    {
        public override void EnableBackgroundSync()
            => UIApplication.SharedApplication
                .SetMinimumBackgroundFetchInterval(MinimumBackgroundFetchInterval.TotalSeconds);

        public override void DisableBackgroundSync()
            => UIApplication.SharedApplication
                .SetMinimumBackgroundFetchInterval(UIApplication.BackgroundFetchIntervalNever);
    }
}
