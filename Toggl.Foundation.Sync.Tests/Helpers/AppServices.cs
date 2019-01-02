using System;
using System.Reactive.Concurrency;
using MvvmCross.Navigation;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Sync.Tests.Helpers
{
    public sealed class AppServices
    {
        private readonly TimeSpan retryLimit = TimeSpan.FromSeconds(60);

        private readonly TimeSpan minimumTimeInBackgroundForFullSync = TimeSpan.FromMinutes(5);

        public IScheduler Scheduler { get; }

        public ITimeService TimeService { get; }

        public IAccessRestrictionStorage AccessRestrictionStorageSubsitute { get; } =
            Substitute.For<IAccessRestrictionStorage>();

        public IMvxNavigationService NavigationServiceSubstitute { get; } =
            Substitute.For<IMvxNavigationService>();

        public IErrorHandlingService ErrorHandlingService { get; }

        public IAnalyticsService AnalyticsServiceSubstitute { get; } = Substitute.For<IAnalyticsService>();

        public ILastTimeUsageStorage LastTimeUsageStorageSubstitute { get; } = Substitute.For<ILastTimeUsageStorage>();

        public INotificationService NotificationServiceSubstitute { get; } = Substitute.For<INotificationService>();

        public IApplicationShortcutCreator ApplicationShortcutCreatorSubstitute { get; } =
            Substitute.For<IApplicationShortcutCreator>();

        public ISyncManager SyncManager { get; }

        public AppServices(ITogglApi api, ITogglDatabase database)
        {
            Scheduler = System.Reactive.Concurrency.Scheduler.Default;
            TimeService = new TimeService(Scheduler);
            ErrorHandlingService = new ErrorHandlingService(
                NavigationServiceSubstitute, AccessRestrictionStorageSubsitute);

            ISyncManager createSyncManager(ITogglDataSource dataSource)
                => TogglSyncManager.CreateSyncManager(
                    database,
                    api,
                    dataSource,
                    TimeService,
                    AnalyticsServiceSubstitute,
                    LastTimeUsageStorageSubstitute,
                    Scheduler);

            var togglDataSource = new TogglDataSource(
                api,
                database,
                TimeService,
                ErrorHandlingService,
                createSyncManager,
                NotificationServiceSubstitute,
                ApplicationShortcutCreatorSubstitute,
                AnalyticsServiceSubstitute);

            SyncManager = togglDataSource.SyncManager;
        }
    }
}
