using System;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using static Toggl.Multivac.WorkspaceFeatureId;

namespace Toggl.Foundation.Interactors
{
    internal sealed class IsBillableAvailableForProjectInteractor : WorkspaceHasFeatureInteractor<bool>
    {
        private readonly long projectId;

        public IsBillableAvailableForProjectInteractor(IInteractorFactory interactorFactory, long projectId)
            : base (interactorFactory)
        {
            this.projectId = projectId;
        }

        public override IObservable<bool> Execute()
            => InteractorFactory.GetProjectById(projectId)
                .Execute()
                .SelectMany(project => CheckIfFeatureIsEnabled(project.WorkspaceId, Pro));
    }
}
