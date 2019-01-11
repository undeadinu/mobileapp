using System;
using System.Collections.Generic;
using Toggl.Foundation.Interactors.Generic;
using Toggl.Foundation.Models.Interfaces;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Interactors
{
    public sealed partial class InteractorFactory
    {
        public IInteractor<IObservable<IEnumerable<IThreadSafeClient>>> GetAllClientsInWorkspace(long workspaceId)
            => new GetAllClientsInWorkspaceInteractor(dataSource.Clients, workspaceId);

        public IInteractor<IObservable<IThreadSafeClient>> CreateClient(string clientName, long workspaceId)
            => new CreateClientInteractor(idProvider, timeService, dataSource.Clients, clientName, workspaceId);

        public IInteractor<IObservable<IThreadSafeClient>> GetClientById(long id)
            => new GetByIdInteractor<IThreadSafeClient, IDatabaseClient>(dataSource.Clients, id);
    }
}
