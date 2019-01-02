using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Android.App;
using Android.Support.V4.App;
using Toggl.Foundation.Services;
using Toggl.Giskard.Extensions;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Services
{
    public sealed class NotificationServiceAndroid : INotificationService
    {

        public IObservable<Unit> Schedule(IImmutableList<Multivac.Notification> notifications)
            => Observable.Start(() =>
                {
                    var context = Application.Context;
                    var notificationIntent = context.PackageManager.GetLaunchIntentForPackage(context.PackageName);
                    notificationIntent.SetPackage(null);
                    var pendingIntent = PendingIntent.GetActivity(context, 0, notificationIntent, 0);
                    var notificationManager = NotificationManager.FromContext(context);
                    var notificationsBuilder = context.CreateNotificationBuilderWithDefaultChannel(notificationManager);

                    notifications.Select(notification =>
                        (
                            notification.Id.GetHashCode(), // this is a temp measure, might be better to change this in the future
                            notificationsBuilder.SetAutoCancel(true)
                                .SetContentIntent(pendingIntent)
                                .SetContentTitle(notification.Title)
                                .SetSmallIcon(Resource.Drawable.ic_icon_running)
                                .SetContentText(notification.Description)
                                .Build()
                        )
                    ).ForEach(notificationManager.Notify);
                });

        public IObservable<Unit> UnscheduleAllNotifications()
            => Observable.Start(() =>
                {
                    var context = Application.Context;
                    var notificationManager = NotificationManagerCompat.From(context);
                    notificationManager.CancelAll();
                });
    }
}
