using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Network;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Diagnostics;

namespace Toggl.Foundation.Tests
{
    public abstract class BaseInteractorTests
    {
        protected IIdProvider IdProvider { get; } = Substitute.For<IIdProvider>();
        protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();
        protected ITogglDataSource DataSource { get; } = Substitute.For<ITogglDataSource>();
        protected IUserPreferences UserPreferences { get; } = Substitute.For<IUserPreferences>();
        protected IAnalyticsService AnalyticsService { get; } = Substitute.For<IAnalyticsService>();
        protected IIntentDonationService IntentDonationService { get; } = Substitute.For<IIntentDonationService>();
        protected IPlatformInfo PlatformInfo { get; } = Substitute.For<IPlatformInfo>();
        protected INotificationService NotificationService { get; } = Substitute.For<INotificationService>();
        protected ILastTimeUsageStorage LastTimeUsageStorage { get; } = Substitute.For<ILastTimeUsageStorage>();
        protected IApplicationShortcutCreator ApplicationShortcutCreator { get; }
            = Substitute.For<IApplicationShortcutCreator>();
        protected UserAgent UserAgent { get; } = new UserAgent("Tests", "0.0");
        protected ICalendarService CalendarService { get; } = Substitute.For<ICalendarService>();
        protected ISyncManager SyncManager { get; } = Substitute.For<ISyncManager>();
        protected IStopwatchProvider StopwatchProvider { get; } = Substitute.For<IStopwatchProvider>();

        protected IInteractorFactory InteractorFactory { get; }

        protected BaseInteractorTests()
        {
            InteractorFactory = new InteractorFactory(
                IdProvider,
                TimeService,
                DataSource,
                UserPreferences,
                AnalyticsService,
                NotificationService,
                IntentDonationService,
                ApplicationShortcutCreator,
                LastTimeUsageStorage,
                PlatformInfo,
                UserAgent,
                CalendarService,
                SyncManager,
                StopwatchProvider
            );
        }
    }
}
