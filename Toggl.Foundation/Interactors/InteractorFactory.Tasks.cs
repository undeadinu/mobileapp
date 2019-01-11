using System;
using Toggl.Foundation.Interactors.Generic;
using Toggl.Foundation.Models.Interfaces;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Interactors
{
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        public IInteractor<IObservable<IThreadSafeTask>> GetTaskById(long id)
            => new GetByIdInteractor<IThreadSafeTask, IDatabaseTask>(dataSource.Tasks, id);
    }
}
