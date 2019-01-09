using System;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Login;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.Interactors.UserAccess
{
    public sealed class LogoutInteractor : IInteractor<IObservable<Unit>>
    {
        private readonly IAnalyticsService analyticsService;
        private readonly INotificationService notificationService;
        private readonly IApplicationShortcutCreator shortcutCreator;
        private readonly ISyncManager syncManager;
        private readonly ITogglDatabase database;
        private readonly IUserPreferences userPreferences;
        private readonly IPrivateSharedStorageService privateSharedStorageService;
        private readonly IIntentDonationService intentDonationService;
        private readonly IUserAccessManager userAccessManager;
        private readonly LogoutSource source;

        public LogoutInteractor(
            IAnalyticsService analyticsService,
            INotificationService notificationService,
            IApplicationShortcutCreator shortcutCreator,
            ISyncManager syncManager,
            ITogglDatabase database,
            IUserPreferences userPreferences,
            IPrivateSharedStorageService privateSharedStorageService,
            IIntentDonationService intentDonationService,
            IUserAccessManager userAccessManager,
            LogoutSource source)
        {
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(notificationService, nameof(notificationService));
            Ensure.Argument.IsNotNull(shortcutCreator, nameof(shortcutCreator));
            Ensure.Argument.IsNotNull(syncManager, nameof(syncManager));
            Ensure.Argument.IsNotNull(database, nameof(database));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(privateSharedStorageService, nameof(privateSharedStorageService));
            Ensure.Argument.IsNotNull(intentDonationService, nameof(intentDonationService));
            Ensure.Argument.IsNotNull(userAccessManager, nameof(userAccessManager));
            Ensure.Argument.IsADefinedEnumValue(source, nameof(source));

            this.analyticsService = analyticsService;
            this.shortcutCreator = shortcutCreator;
            this.notificationService = notificationService;
            this.syncManager = syncManager;
            this.database = database;
            this.userPreferences = userPreferences;
            this.privateSharedStorageService = privateSharedStorageService;
            this.intentDonationService = intentDonationService;
            this.userAccessManager = userAccessManager;
            this.source = source;
        }

        public IObservable<Unit> Execute()
            => syncManager.Freeze()
                .FirstAsync()
                .SelectMany(_ => database.Clear())
                .Do(shortcutCreator.OnLogout)
                .Do(userPreferences.Reset)
                .Do(privateSharedStorageService.ClearAll)
                .Do(intentDonationService.ClearAll)
                .Do(_ => analyticsService.Logout.Track(source))
                .SelectMany(_ =>
                    notificationService
                        .UnscheduleAllNotifications()
                        .Catch(Observable.Return(Unit.Default)))
                .Do(_ => userAccessManager.UserLoggedOut.OnNext(Unit.Default))
                .FirstAsync();
    }
}
