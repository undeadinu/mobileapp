using System;
using System.Reactive;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Reports;
using Toggl.Foundation.Sync;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave.ApiClients;

namespace Toggl.Foundation.DataSources
{
    public interface ITogglDataSource
    {
        ITimeEntriesSource TimeEntries { get; }
        ISingletonDataSource<IThreadSafeUser> User { get; }
        IDataSource<IThreadSafeTag, IDatabaseTag> Tags { get; }
        IDataSource<IThreadSafeTask, IDatabaseTask> Tasks { get; }
        IDataSource<IThreadSafeClient, IDatabaseClient> Clients { get; }
        ISingletonDataSource<IThreadSafePreferences> Preferences { get; }
        IDataSource<IThreadSafeProject, IDatabaseProject> Projects { get; }
        IObservableDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> Workspaces { get; }
        IDataSource<IThreadSafeWorkspaceFeatureCollection, IDatabaseWorkspaceFeatureCollection> WorkspaceFeatures { get; }

        ISyncManager SyncManager { get; }

        IReportsProvider ReportsProvider { get; }

        IFeedbackApi FeedbackApi { get; }

        IObservable<bool> HasUnsyncedData();

        IObservable<Unit> Logout();
    }
}
