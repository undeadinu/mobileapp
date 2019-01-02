using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Interactors
{
    [Preserve(AllMembers = true)]
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        private readonly IIdProvider idProvider;
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IUserPreferences userPreferences;
        private readonly IAnalyticsService analyticsService;
        private readonly INotificationService notificationService;
        private readonly IIntentDonationService intentDonationService;
        private readonly IApplicationShortcutCreator shortcutCreator;
        private readonly ILastTimeUsageStorage lastTimeUsageStorage;
        private readonly IPlatformInfo platformInfo;
        private readonly UserAgent userAgent;
        private readonly ICalendarService calendarService;
        private readonly ISyncManager syncManager;
        private readonly IStopwatchProvider stopwatchProvider;

        public InteractorFactory(
            IIdProvider idProvider,
            ITimeService timeService,
            ITogglDataSource dataSource,
            IUserPreferences userPreferences,
            IAnalyticsService analyticsService,
            INotificationService notificationService,
            IIntentDonationService intentDonationService,
            IApplicationShortcutCreator shortcutCreator,
            ILastTimeUsageStorage lastTimeUsageStorage,
            IPlatformInfo platformInfo,
            UserAgent userAgent,
            ICalendarService calendarService,
            ISyncManager syncManager,
            IStopwatchProvider stopwatchProvider)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(idProvider, nameof(idProvider));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(shortcutCreator, nameof(shortcutCreator));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(notificationService, nameof(notificationService));
            Ensure.Argument.IsNotNull(intentDonationService, nameof(intentDonationService));
            Ensure.Argument.IsNotNull(lastTimeUsageStorage, nameof(lastTimeUsageStorage));
            Ensure.Argument.IsNotNull(platformInfo, nameof(platformInfo));
            Ensure.Argument.IsNotNull(userAgent, nameof(userAgent));
            Ensure.Argument.IsNotNull(calendarService, nameof(calendarService));
            Ensure.Argument.IsNotNull(syncManager, nameof(syncManager));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));

            this.dataSource = dataSource;
            this.idProvider = idProvider;
            this.timeService = timeService;
            this.userPreferences = userPreferences;
            this.shortcutCreator = shortcutCreator;
            this.analyticsService = analyticsService;
            this.notificationService = notificationService;
            this.intentDonationService = intentDonationService;
            this.lastTimeUsageStorage = lastTimeUsageStorage;
            this.platformInfo = platformInfo;
            this.userAgent = userAgent;
            this.calendarService = calendarService;
            this.syncManager = syncManager;
            this.stopwatchProvider = stopwatchProvider;
        }
    }
}
