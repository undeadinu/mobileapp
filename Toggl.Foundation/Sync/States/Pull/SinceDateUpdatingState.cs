using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Extensions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.Sync.States.Pull
{
    public sealed class SinceDateUpdatingState<T> : ISyncState<IFetchObservables>
        where T : ILastChangedDatable
    {
        private readonly ISinceParameterRepository sinceParameterRepository;

        public StateResult<IFetchObservables> Finished { get; } = new StateResult<IFetchObservables>();

        public SinceDateUpdatingState(ISinceParameterRepository sinceParameterRepository)
        {
            Ensure.Argument.IsNotNull(sinceParameterRepository, nameof(sinceParameterRepository));

            this.sinceParameterRepository = sinceParameterRepository;
        }

        public IObservable<ITransition> Start(IFetchObservables fetch)
            => fetch.GetList<T>()
                .Do(maybeUpdateSinceDates)
                .SelectValue(Finished.Transition(fetch));

        private void maybeUpdateSinceDates(List<T> entities)
        {
            var since = entities.Max(entity => entity?.At);
            if (since.HasValue)
            {
                sinceParameterRepository.Set<T>(since);
            }
        }
    }
}
