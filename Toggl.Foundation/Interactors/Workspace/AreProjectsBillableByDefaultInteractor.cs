using System;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Interactors
{
    internal sealed class AreProjectsBillableByDefaultInteractor : WorkspaceHasFeatureInteractor<bool?>
    {
        private readonly long workspaceId;

        public AreProjectsBillableByDefaultInteractor(IInteractorFactory interactorFactory, long workspaceId)
            : base(interactorFactory)
        {
            this.workspaceId = workspaceId;
        }

        public override IObservable<bool?> Execute()
            => CheckIfFeatureIsEnabled(workspaceId, WorkspaceFeatureId.Pro)
                .SelectMany(isPro =>
                {
                    if (!isPro)
                        return Observable.Return<bool?>(null);

                    return InteractorFactory.GetWorkspaceById(workspaceId)
                        .Execute()
                        .Select<IDatabaseWorkspace, bool?>(workspace => workspace.ProjectsBillableByDefault);
                });

    }
}
