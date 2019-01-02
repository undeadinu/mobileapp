using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Foundation.Extensions;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Sync.States.Pull
{
    internal sealed class FetchAllSinceState : ISyncState
    {
        private const int sinceDateLimitMonths = 2;
        private const int fetchTimeEntriesForMonths = 2;
        private const int timeEntriesEndDateInclusiveExtraDaysCount = 2;

        private readonly ITogglApi api;
        private readonly ISinceParameterRepository since;
        private readonly ITimeService timeService;
        private readonly ILeakyBucket leakyBucket;
        private readonly IRateLimiter limiter;

        public StateResult<TimeSpan> PreventOverloadingServer { get; } = new StateResult<TimeSpan>();
        public StateResult<IFetchObservables> FetchStarted { get; } = new StateResult<IFetchObservables>();

        public FetchAllSinceState(
            ITogglApi api,
            ISinceParameterRepository since,
            ITimeService timeService,
            ILeakyBucket leakyBucket,
            IRateLimiter limiter)
        {
            this.api = api;
            this.since = since;
            this.timeService = timeService;
            this.leakyBucket = leakyBucket;
            this.limiter = limiter;
        }

        public IObservable<ITransition> Start() => Observable.Create<ITransition>(observer =>
        {
            if (!leakyBucket.TryClaimFreeSlots(numberOfSlots: 9, timeToFreeSlot: out var timeToNextFreeSlot))
            {
                observer.CompleteWith(PreventOverloadingServer.Transition(timeToNextFreeSlot));
                return () => { };
            }

            var observables = fetchInWaves();

            observer.CompleteWith(FetchStarted.Transition(observables));
            return () => { };
        });

        private IFetchObservables fetchInWaves()
        {
            var (workspaces, user, features) = firstWave();
            var (preferences, tags, clients) = secondWave(workspaces, user, features);
            var (projects, timeEntries, tasks) = thirdWave(preferences, tags, clients);

            return new FetchObservables(
                workspaces, features, user, clients, projects, timeEntries, tags, tasks, preferences);
        }

        private (IObservable<List<IWorkspace>>, IObservable<IUser>, IObservable<List<IWorkspaceFeatureCollection>>) firstWave()
        {
            var workspaces =
                limiter.WaitForFreeSlot()
                    .ThenExecute(() => api.Workspaces.GetAll())
                    .ConnectedReplay();

            var user =
                limiter.WaitForFreeSlot()
                    .ThenExecute(() => api.User.Get())
                    .ConnectedReplay();

            var features =
                limiter.WaitForFreeSlot()
                    .ThenExecute(() => api.WorkspaceFeatures.GetAll())
                    .ConnectedReplay();

            return (workspaces, user, features);
        }

        private (IObservable<IPreferences>, IObservable<List<ITag>>, IObservable<List<IClient>>) secondWave(
            IObservable<List<IWorkspace>> workspaces,
            IObservable<IUser> user,
            IObservable<List<IWorkspaceFeatureCollection>> features)
        {
            var preferences =
                workspaces.ThenExecute(limiter.WaitForFreeSlot)
                    .ThenExecute(() => api.Preferences.Get())
                    .ConnectedReplay();

            var tags =
                user.ThenExecute(limiter.WaitForFreeSlot)
                    .ThenExecute(() => fetchRecentIfPossible(api.Tags.GetAllSince, api.Tags.GetAll))
                    .ConnectedReplay();

            var clients =
                features.ThenExecute(limiter.WaitForFreeSlot)
                    .ThenExecute(() => fetchRecentIfPossible(api.Clients.GetAllSince, api.Clients.GetAll))
                    .ConnectedReplay();

            return (preferences, tags, clients);
        }

        private (IObservable<List<IProject>>, IObservable<List<ITimeEntry>>, IObservable<List<ITask>>) thirdWave(
            IObservable<IPreferences> preferences, IObservable<List<ITag>> tags, IObservable<List<IClient>> clients)
        {
            var projects =
                preferences.ThenExecute(limiter.WaitForFreeSlot)
                    .ThenExecute(() => fetchRecentIfPossible(api.Projects.GetAllSince, api.Projects.GetAll))
                    .ConnectedReplay();

            var timeEntries =
                tags.ThenExecute(limiter.WaitForFreeSlot)
                    .ThenExecute(() => fetchRecentIfPossible(api.TimeEntries.GetAllSince, fetchTwoMonthsOfTimeEntries))
                    .ConnectedReplay();

            var tasks =
                clients.ThenExecute(limiter.WaitForFreeSlot)
                    .ThenExecute(() => fetchRecentIfPossible(api.Tasks.GetAllSince, api.Tasks.GetAll))
                    .ConnectedReplay();

            return (projects, timeEntries, tasks);
        }

        private IObservable<List<ITimeEntry>> fetchTwoMonthsOfTimeEntries()
            => api.TimeEntries.GetAll(
                start: timeService.CurrentDateTime.AddMonths(-fetchTimeEntriesForMonths),
                end: timeService.CurrentDateTime.AddDays(timeEntriesEndDateInclusiveExtraDaysCount));

        private IObservable<List<T>> fetchRecentIfPossible<T>(
            Func<DateTimeOffset, IObservable<List<T>>> getAllSince,
            Func<IObservable<List<T>>> getAll)
            where T : ILastChangedDatable
        {
            var threshold = since.Get<T>();
            return threshold.HasValue && isWithinLimit(threshold.Value)
                ? getAllSince(threshold.Value)
                : getAll();
        }

        private bool isWithinLimit(DateTimeOffset threshold)
            => threshold > timeService.CurrentDateTime.AddMonths(-sinceDateLimitMonths);
    }
}
