using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Sync.States.Pull
{
    public class ResetSinceParamsState : ISyncState<IEnumerable<IWorkspace>>
    {
        private readonly ISinceParameterRepository sinceParameterRepository;

        public StateResult<IEnumerable<IWorkspace>> Done { get; } = new StateResult<IEnumerable<IWorkspace>>();

        public ResetSinceParamsState(ISinceParameterRepository sinceParameterRepository)
        {
            Ensure.Argument.IsNotNull(sinceParameterRepository, nameof(sinceParameterRepository));
            this.sinceParameterRepository = sinceParameterRepository;
        }

        public IObservable<ITransition> Start(IEnumerable<IWorkspace> workspaces)
        {
            sinceParameterRepository.Reset();
            return Observable.Return(Done.Transition(workspaces));
        }
    }
}
