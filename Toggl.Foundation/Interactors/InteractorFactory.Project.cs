using System;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Interactors.Generic;
using Toggl.Foundation.Models.Interfaces;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Interactors
{
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        public IInteractor<IObservable<bool>> IsBillableAvailableForProject(long projectId)
            => new IsBillableAvailableForProjectInteractor(this, projectId);

        public IInteractor<IObservable<bool>> ProjectDefaultsToBillable(long projectId)
            => new ProjectDefaultsToBillableInteractor(this, projectId);

        public IInteractor<IObservable<IThreadSafeProject>> CreateProject(CreateProjectDTO dto)
            => new CreateProjectInteractor(idProvider, timeService, dataSource.Projects, dto);

        public IInteractor<IObservable<IThreadSafeProject>> GetProjectById(long id)
            => new GetByIdInteractor<IThreadSafeProject, IDatabaseProject>(dataSource.Projects, id);
    }
}
