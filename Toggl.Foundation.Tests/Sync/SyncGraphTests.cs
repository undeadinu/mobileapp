using System;
using System.Reactive;
using System.Reactive.Concurrency;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Sync;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;
using Xunit;

namespace Toggl.Foundation.Tests.Sync
{
    public sealed class SyncGraphTests
    {
        public sealed class TheSyncGraph
        {
            [Fact, LogIfTooSlow]
            public void HasNoLooseEnds()
            {
                var configurator = configureTogglSyncGraph();

                var looseEnds = configurator.GetAllLooseEndStateResults();

                looseEnds.Should().BeEmpty();
            }
        }

        private static TestConfigurator configureTogglSyncGraph()
        {
            var configurator = new TestConfigurator();
            var entryPoints = new StateMachineEntryPoints();

            configurator.AllDistinctStatesInOrder.Add(entryPoints);

            TogglSyncManager.ConfigureTransitions(
                configurator,
                Substitute.For<ITogglDatabase>(),
                Substitute.For<ITogglApi>(),
                Substitute.For<ITogglDataSource>(),
                Substitute.For<IScheduler>(),
                Substitute.For<ITimeService>(),
                Substitute.For<IAnalyticsService>(),
                entryPoints,
                Substitute.For<ISyncStateQueue>()
            );

            return configurator;
        }
    }
}
