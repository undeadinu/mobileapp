using System;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Toggl.Foundation.Services;
using Toggl.Multivac;

namespace Toggl.Giskard.Startup
{
    public sealed class ApplicationLifecycleObserver : Java.Lang.Object, Application.IActivityLifecycleCallbacks, IComponentCallbacks2
    {
        private readonly IBackgroundService backgroundService;
        private bool isInBackground = true;

        public ApplicationLifecycleObserver(IBackgroundService backgroundService)
        {
            Ensure.Argument.IsNotNull(backgroundService, nameof(backgroundService));
            this.backgroundService = backgroundService;
        }

        public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
        {
        }

        public void OnActivityDestroyed(Activity activity)
        {
        }

        public void OnActivityPaused(Activity activity)
        {
        }

        public void OnActivityResumed(Activity activity)
        {
            if (isInBackground)
            {
                isInBackground = false;
                backgroundService.EnterForeground();
            }
        }

        public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {
        }

        public void OnActivityStarted(Activity activity)
        {
        }

        public void OnActivityStopped(Activity activity)
        {
        }

        public void OnConfigurationChanged(Configuration newConfig)
        {
        }

        public void OnLowMemory()
        {
        }

        public void OnTrimMemory(TrimMemory level)
        {
            if (level == TrimMemory.UiHidden)
            {
                isInBackground = true;
                backgroundService.EnterBackground();
            }
        }
    }
}
