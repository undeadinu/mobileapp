using System;
using Toggl.Foundation.DataSources;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using static Toggl.Multivac.WorkspaceFeatureId;

namespace Toggl.Foundation.Interactors
{
    internal sealed class AreCustomColorsEnabledForWorkspaceInteractor : WorkspaceHasFeatureInteractor<bool>
    {
        private readonly long workspaceId;

        public AreCustomColorsEnabledForWorkspaceInteractor(IInteractorFactory interactorFactory, long workspaceId)
            : base(interactorFactory)
        {
            this.workspaceId = workspaceId;
        }

        public override IObservable<bool> Execute()
            => CheckIfFeatureIsEnabled(workspaceId, Pro);
    }
}
