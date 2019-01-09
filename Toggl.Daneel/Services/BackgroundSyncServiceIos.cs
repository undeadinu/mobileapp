using Toggl.Foundation.Services;
using UIKit;

namespace Toggl.Daneel.Services
{
    public sealed class BackgroundSyncServiceIos : BaseBackgroundSyncService
    {
        public override void EnableBackgroundSync()
        {
            configureInterval(MinimumBackgroundFetchInterval.TotalSeconds);
        }

        public override void DisableBackgroundSync()
        {
            configureInterval(UIApplication.BackgroundFetchIntervalNever);
        }

        private static void configureInterval(double interval)
        {
            UIApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                UIApplication.SharedApplication.SetMinimumBackgroundFetchInterval(interval);
            });
        }
    }
}
