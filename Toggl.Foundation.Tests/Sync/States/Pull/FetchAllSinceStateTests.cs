using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Sync.States.Pull;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.Pull
{
    public sealed class FetchAllSinceStateTests
    {
        public sealed class TheStartMethod
        {
            private readonly ISinceParameterRepository sinceParameters;
            private readonly ITogglApi api;
            private readonly ITimeService timeService;
            private readonly ILeakyBucket leakyBucket;
            private readonly IRateLimiter rateLimiter;
            private readonly FetchAllSinceState state;
            private readonly DateTimeOffset now = new DateTimeOffset(2017, 02, 15, 13, 50, 00, TimeSpan.Zero);

            public TheStartMethod()
            {
                sinceParameters = Substitute.For<ISinceParameterRepository>();
                api = Substitute.For<ITogglApi>();
                timeService = Substitute.For<ITimeService>();
                leakyBucket = Substitute.For<ILeakyBucket>();
                leakyBucket.TryClaimFreeSlots(Arg.Any<int>(), out _).Returns(true);
                rateLimiter = Substitute.For<IRateLimiter>();
                rateLimiter.WaitForFreeSlot().Returns(Observable.Return(Unit.Default));
                timeService.CurrentDateTime.Returns(now);
                state = new FetchAllSinceState(api, sinceParameters, timeService, leakyBucket, rateLimiter);
            }

            [Fact, LogIfTooSlow]
            public void EmitsTransitionToFetchStartedResult()
            {
                var transition = state.Start().SingleAsync().Wait();

                transition.Result.Should().Be(state.FetchStarted);
            }

            [Fact, LogIfTooSlow]
            public void MakesNoApiCallsBeforeSubscription()
            {
                state.Start();

                var w = api.DidNotReceive().Workspaces;
                var wf = api.DidNotReceive().WorkspaceFeatures;
                var u = api.DidNotReceive().User;
                var c = api.DidNotReceive().Clients;
                var p = api.DidNotReceive().Projects;
                var t = api.DidNotReceive().TimeEntries;
                var ta = api.DidNotReceive().Tags;
                var ts = api.DidNotReceive().Tasks;
                var pr = api.DidNotReceive().Preferences;
            }

            [Fact, LogIfTooSlow]
            public void SendsRequestsInWavesWhenRateLimiterAllocatesASlotAfterSubscription()
            {
                var scheduler = new TestScheduler();
                var delay = TimeSpan.FromSeconds(1);
                rateLimiter.WaitForFreeSlot().Returns(Observable.Return(Unit.Default).Delay(delay, scheduler));

                api.Workspaces.GetAll().Returns(Observable.Return<List<IWorkspace>>(null));
                api.WorkspaceFeatures.GetAll().Returns(Observable.Return<List<IWorkspaceFeatureCollection>>(null));
                api.User.Get().Returns(Observable.Return<IUser>(null));
                api.Clients.GetAll().Returns(Observable.Return<List<IClient>>(null));
                api.Projects.GetAll().Returns(Observable.Return<List<IProject>>(null));
                api.TimeEntries.GetAll(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Return<List<ITimeEntry>>(null));
                api.Tasks.GetAll().Returns(Observable.Return<List<ITask>>(null));
                api.Tags.GetAll().Returns(Observable.Return<List<ITag>>(null));
                api.Preferences.Get().Returns(Observable.Return<IPreferences>(null));

                state.Start().Subscribe();

                // before the first wave
                api.Workspaces.DidNotReceive().GetAll();
                api.WorkspaceFeatures.DidNotReceive().GetAll();
                api.User.DidNotReceive().Get();

                // first wave
                scheduler.AdvanceBy(delay.Ticks);

                api.Workspaces.Received().GetAll();
                api.WorkspaceFeatures.Received().GetAll();
                api.User.Received().Get();

                api.Preferences.DidNotReceive().Get();
                api.Clients.DidNotReceive().GetAll();
                api.Clients.DidNotReceive().GetAllSince(Arg.Any<DateTimeOffset>());
                api.Tags.DidNotReceive().GetAll();
                api.Tags.DidNotReceive().GetAllSince(Arg.Any<DateTimeOffset>());

                // second wave
                scheduler.AdvanceBy(delay.Ticks);

                api.Preferences.Received().Get();
                api.Clients.Received().GetAll();
                api.Tags.Received().GetAll();

                api.Tasks.DidNotReceive().GetAll();
                api.Tasks.DidNotReceive().GetAllSince(Arg.Any<DateTimeOffset>());
                api.Projects.DidNotReceive().GetAll();
                api.Projects.DidNotReceive().GetAllSince(Arg.Any<DateTimeOffset>());
                api.TimeEntries.DidNotReceive().GetAll(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>());
                api.TimeEntries.DidNotReceive().GetAllSince(Arg.Any<DateTimeOffset>());

                // third wave
                scheduler.AdvanceBy(delay.Ticks);

                api.Tasks.Received().GetAll();
                api.Projects.Received().GetAll();
                api.TimeEntries.Received().GetAll(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>());
            }

            [Property]
            public void MakesCorrectCallsWithSinceThresholdsWhenSinceIsLessThanTwoMonthsInThePast(int seed)
            {
                var rnd = new Random(seed);
                var percent = rnd.NextDouble();

                var now = timeService.CurrentDateTime;
                var twoMonths = (now.AddMonths(2) - now);
                var seconds = twoMonths.TotalSeconds * percent;
                var since = now.AddSeconds(-seconds);

                sinceParameters.Get<IClient>().Returns(since);
                sinceParameters.Get<IProject>().Returns(since);
                sinceParameters.Get<ITask>().Returns(since);
                sinceParameters.Get<ITag>().Returns(since);
                sinceParameters.Get<IWorkspace>().Returns(since);
                sinceParameters.Get<ITimeEntry>().Returns(since);

                state.Start().Wait();

                api.Workspaces.Received().GetAll();
                api.Clients.Received().GetAllSince(since);
                api.Projects.Received().GetAllSince(since);
                api.TimeEntries.Received().GetAllSince(since);
                api.Tasks.Received().GetAllSince(since);
                api.Tags.Received().GetAllSince(since);
            }

            [Fact, LogIfTooSlow]
            public void MakesApiCallsWithoutTheSinceParameterWhenTheThresholdIsMoreThanTwoMonthsInThePast()
            {
                var now = timeService.CurrentDateTime;

                sinceParameters.Get<IClient>().Returns(now.AddMonths(-3));
                sinceParameters.Get<IProject>().Returns(now.AddMonths(-4));
                sinceParameters.Get<ITask>().Returns(now.AddMonths(-5));
                sinceParameters.Get<ITag>().Returns(now.AddMonths(-6));
                sinceParameters.Get<IWorkspace>().Returns(now.AddMonths(-7));
                sinceParameters.Get<ITimeEntry>().Returns(now.AddMonths(-8));

                state.Start().Wait();

                api.Workspaces.Received().GetAll();
                api.Clients.Received().GetAll();
                api.Projects.Received().GetAll();
                api.TimeEntries.Received().GetAll(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>());
                api.Tasks.Received().GetAll();
                api.Tags.Received().GetAll();
            }

            [Fact, LogIfTooSlow]
            public void MakesCorrectCallsWithoutSinceThresholds()
            {
                state.Start().Wait();

                api.Workspaces.Received().GetAll();
                api.Clients.Received().GetAll();
                api.Projects.Received().GetAll();
                api.TimeEntries.Received().GetAll(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>());
                api.Tasks.Received().GetAll();
                api.Tags.Received().GetAll();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsReplayingApiCallObservables()
            {
                api.Workspaces.GetAll().Returns(Observable.Return<List<IWorkspace>>(null));
                api.WorkspaceFeatures.GetAll().Returns(Observable.Return<List<IWorkspaceFeatureCollection>>(null));
                api.User.Get().Returns(Observable.Return<IUser>(null));
                api.Clients.GetAll().Returns(Observable.Return<List<IClient>>(null));
                api.Projects.GetAll().Returns(Observable.Return<List<IProject>>(null));
                api.TimeEntries.GetAll(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Return<List<ITimeEntry>>(null));
                api.Tasks.GetAll().Returns(Observable.Return<List<ITask>>(null));
                api.Tags.GetAll().Returns(Observable.Return<List<ITag>>(null));
                api.Preferences.Get().Returns(Observable.Return<IPreferences>(null));

                var transition = (Transition<IFetchObservables>)state.Start().SingleAsync().Wait();

                var observables = transition.Parameter;
                observables.GetList<IWorkspace>().SingleAsync().Wait().Should().BeNull();
                observables.GetList<IWorkspaceFeatureCollection>().SingleAsync().Wait().Should().BeNull();
                observables.GetSingle<IUser>().SingleAsync().Wait().Should().BeNull();
                observables.GetList<IClient>().SingleAsync().Wait().Should().BeNull();
                observables.GetList<IProject>().SingleAsync().Wait().Should().BeNull();
                observables.GetList<ITimeEntry>().SingleAsync().Wait().Should().BeNull();
                observables.GetList<ITask>().SingleAsync().Wait().Should().BeNull();
                observables.GetList<ITag>().SingleAsync().Wait().Should().BeNull();
                observables.GetSingle<IPreferences>().SingleAsync().Wait().Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public async Task FetchesTwoMonthsOfTimeEntriesDataIncludingTwoDaysAfterNow()
            {
                sinceParameters.Get<ITimeEntry>().Returns(now.AddMonths(-8));

                await state.Start().SingleAsync();

                var max = TimeSpan.FromDays(62);
                var min = TimeSpan.FromDays(59);

                await api.TimeEntries.Received().GetAll(
                    Arg.Is<DateTimeOffset>(start => min <= now - start && now - start <= max), Arg.Is(now.AddDays(2)));
            }

            [Property]
            public void ReturnsPreventServerOverloadWithCorrectDelayWhenTheLeakyBucketIsFull(TimeSpan delay)
            {
                leakyBucket.TryClaimFreeSlots(Arg.Any<int>(), out _)
                    .Returns(x =>
                    {
                        x[1] = delay;
                        return false;
                    });

                var transition = state.Start().SingleAsync().Wait();

                transition.Result.Should().Be(state.PreventOverloadingServer);
                var parameter = ((Transition<TimeSpan>)transition).Parameter;
            }
        }
    }
}
