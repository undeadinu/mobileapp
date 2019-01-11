using System;
using Toggl.Foundation.Interactors.Generic;
using Toggl.Foundation.Models.Interfaces;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Interactors
{
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        public IInteractor<IObservable<IThreadSafeWorkspaceFeatureCollection>> GetWorkspaceFeaturesById(long id)
            => new GetByIdInteractor<IThreadSafeWorkspaceFeatureCollection, IDatabaseWorkspaceFeatureCollection>(dataSource.WorkspaceFeatures, id);
    }
}
