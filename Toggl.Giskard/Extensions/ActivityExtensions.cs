using Android.App;
using Android.App.Job;
using Android.Content;
using Android.Graphics;
using Android.Support.V4.App;
using Android.Util;
using Android.Views;
using Toggl.Giskard.Helper;
using Toggl.Giskard.Services;

namespace Toggl.Giskard.Extensions
{
    public static class ActivityExtensions
    {
        private static readonly Color lollipopFallbackStatusBarColor = Color.ParseColor("#2C2C2C");
        private static readonly long[] noNotificationVibrationPattern = { 0L, 0L };
        private static readonly string defaultChannelId = "Toggl";
        private static readonly string defaultChannelName = "Toggl";
        private static readonly string defaultChannelDescription = "Toggl notifications";

        public static void ChangeStatusBarColor(this Activity activity, Color color, bool useDarkIcons = false)
        {
            var window = activity.Window;
            window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            window.ClearFlags(WindowManagerFlags.TranslucentStatus);
            window.SetStatusBarColor(color);

            if (MarshmallowApis.AreNotAvailable)
            {
                if (color == Color.White && useDarkIcons)
                {
                    window.SetStatusBarColor(lollipopFallbackStatusBarColor);
                }
                return;
            }

            window.DecorView.SystemUiVisibility =
                (StatusBarVisibility)(useDarkIcons ? SystemUiFlags.LightStatusBar : SystemUiFlags.Visible);
        }

        public static (int widthPixels, int heightPixels, bool isLargeScreen) GetMetrics(this Activity activity, Context context = null)
        {
            const int largeScreenThreshold = 360;

            context = context ?? activity.ApplicationContext;

            var displayMetrics = new DisplayMetrics();
            activity.WindowManager.DefaultDisplay.GetMetrics(displayMetrics);

            var isLargeScreen = displayMetrics.WidthPixels > largeScreenThreshold.DpToPixels(context);

            return (displayMetrics.WidthPixels, displayMetrics.HeightPixels, isLargeScreen);
        }

        public static JobInfo CreateBackgroundSyncJobInfo(this Context context, long periodicity)
        {
            var javaClass = Java.Lang.Class.FromType(typeof(BackgroundSyncJobSchedulerService));
            var component = new ComponentName(context, javaClass);

            var builder = new JobInfo.Builder(BackgroundSyncJobSchedulerService.JobId, component)
                .SetRequiredNetworkType(NetworkType.Any)
                .SetPeriodic(periodicity)
                .SetPersisted(true);

            var jobInfo = builder.Build();
            return jobInfo;
        }

        public static void SetDefaultDialogLayout(this Window window, Activity activity, Context context, int heightDp)
        {
            const int smallScreenWidth = 312;
            const int largeScreenMargins = 72;

            var (widthPixels, heightPixels, isLargeScreen) = activity.GetMetrics(context);

            var width = isLargeScreen
                ? widthPixels - largeScreenMargins.DpToPixels(context)
                : smallScreenWidth.DpToPixels(context);

            var height = heightDp >= 0
                ? heightDp.DpToPixels(context)
                : heightDp;

            window.SetLayout(width, height);
        }

        public static NotificationCompat.Builder CreateNotificationBuilderWithDefaultChannel(this Context context,
            NotificationManager notificationManager)
        {
            if (OreoApis.AreAvailable)
            {
                var channel = new NotificationChannel(defaultChannelId, defaultChannelName, NotificationImportance.Low);
                channel.Description = defaultChannelDescription;
                channel.EnableVibration(false);
                channel.SetVibrationPattern(noNotificationVibrationPattern);
                notificationManager.CreateNotificationChannel(channel);
            }

            var notificationBuilder = new NotificationCompat.Builder(context, defaultChannelId);
            notificationBuilder.SetVibrate(noNotificationVibrationPattern);

            return notificationBuilder;
        }

        public static void CancelAllNotifications(this Context context)
        {
            var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
            notificationManager?.CancelAll();
        }
    }
}
