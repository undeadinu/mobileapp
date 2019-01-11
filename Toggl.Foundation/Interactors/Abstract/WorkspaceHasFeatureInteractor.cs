using System;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Interactors
{
    internal abstract class WorkspaceHasFeatureInteractor<TValue> : IInteractor<IObservable<TValue>>
    {
        protected IInteractorFactory InteractorFactory { get; }

        public WorkspaceHasFeatureInteractor(IInteractorFactory interactorFactory)
        {
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));

            InteractorFactory = interactorFactory;
        }

        public IObservable<bool> CheckIfFeatureIsEnabled(long workspaceId, WorkspaceFeatureId featureId)
            => InteractorFactory.GetWorkspaceFeaturesById(workspaceId)
                .Execute()
                .Select(featureCollection => featureCollection.IsEnabled(featureId));

        public abstract IObservable<TValue> Execute();
    }
}
