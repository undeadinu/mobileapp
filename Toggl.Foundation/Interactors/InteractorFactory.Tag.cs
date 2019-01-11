using System;
using Toggl.Foundation.Interactors.Generic;
using Toggl.Foundation.Models.Interfaces;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Interactors
{
    public sealed partial class InteractorFactory
    {
        public IInteractor<IObservable<IThreadSafeTag>> CreateTag(string tagName, long workspaceId)
            => new CreateTagInteractor(idProvider, timeService, dataSource.Tags, tagName, workspaceId);

        public IInteractor<IObservable<IThreadSafeTag>> GetTagById(long id)
            => new GetByIdInteractor<IThreadSafeTag, IDatabaseTag>(dataSource.Tags, id);
    }
}
