using System;
using Toggl.Foundation.DataSources;
using static Toggl.Multivac.WorkspaceFeatureId;

namespace Toggl.Foundation.Interactors
{
    internal sealed class WorkspaceAllowsBillableRatesInteractor : WorkspaceHasFeatureInteractor<bool>
    {
        private readonly long workspaceId;

        public WorkspaceAllowsBillableRatesInteractor(IInteractorFactory interactorFactory, long workspaceId)
            : base(interactorFactory)
        {
            this.workspaceId = workspaceId;
        }

        public override IObservable<bool> Execute()
            => CheckIfFeatureIsEnabled(workspaceId, Pro);
    }
}
