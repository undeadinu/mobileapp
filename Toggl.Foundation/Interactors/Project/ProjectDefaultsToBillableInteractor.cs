using System;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Multivac;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Interactors
{
    internal sealed class ProjectDefaultsToBillableInteractor : IInteractor<IObservable<bool>>
    {
        private readonly long projectId;
        private readonly IInteractorFactory interactorFactory;

        public ProjectDefaultsToBillableInteractor(IInteractorFactory interactorFactory, long projectId)
        {
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));

            this.projectId = projectId;
            this.interactorFactory = interactorFactory;
        }

        public IObservable<bool> Execute()
            => interactorFactory.GetProjectById(projectId)
                .Execute()
                .Select(project => project.Billable ?? false);
    }
}
