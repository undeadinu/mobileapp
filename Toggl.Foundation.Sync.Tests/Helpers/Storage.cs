using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Realms;
using Toggl.Foundation.Sync.Tests.State;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.PrimeRadiant.Realm;

namespace Toggl.Foundation.Sync.Tests.Helpers
{
    public sealed class Storage
    {
        public ITogglDatabase Database { get; }

        public Storage()
        {
            Database = new Database();
        }

        public async Task<DatabaseState> LoadCurrentState()
        {
            var user = Models.User.From(await Database.User.Single());
            var clients = await Database.Clients.GetAll().Select(c => c.Select(Models.Client.From));
            var projects = await Database.Projects.GetAll().Select(p => p.Select(Models.Project.From));
            var preferences = Models.Preferences.From(await Database.Preferences.Single());
            var tags = await Database.Tags.GetAll().Select(t => t.Select(Models.Tag.From));
            var tasks = await Database.Tasks.GetAll().Select(t => t.Select(Models.Task.From));
            var timeEntries = await Database.TimeEntries.GetAll().Select(te => te.Select(Models.TimeEntry.From));
            var workspaces = await Database.Workspaces.GetAll().Select(ws => ws.Select(Models.Workspace.From));
            var sinceParameters = new Dictionary<Type, DateTimeOffset?>
            {
                [typeof(IClient)] = Database.SinceParameters.Get<IClient>(),
                [typeof(IProject)] = Database.SinceParameters.Get<IProject>(),
                [typeof(ITag)] = Database.SinceParameters.Get<ITag>(),
                [typeof(ITask)] = Database.SinceParameters.Get<ITask>(),
                [typeof(ITimeEntry)] = Database.SinceParameters.Get<ITimeEntry>(),
                [typeof(IWorkspace)] = Database.SinceParameters.Get<IWorkspace>()
            };

            return new DatabaseState(
                user, clients, projects, preferences, tags, tasks, timeEntries, workspaces, sinceParameters);
        }

        public async Task Store(DatabaseState databaseState)
        {
            if (databaseState.Workspaces.Count > 0)
                await databaseState.Workspaces.Select(Database.Workspaces.Create).Merge();

            if (databaseState.User != null)
                await Database.User.Create(databaseState.User);

            if (databaseState.Preferences != null)
                await Database.Preferences.Create(databaseState.Preferences);

            if (databaseState.Tags.Count > 0)
                await databaseState.Tags.Select(Database.Tags.Create).Merge();

            if (databaseState.Clients.Count > 0)
                await databaseState.Clients.Select(Database.Clients.Create).Merge();

            if (databaseState.Projects.Count > 0)
                await databaseState.Projects.Select(Database.Projects.Create).Merge();

            if (databaseState.Tasks.Count > 0)
                await databaseState.Tasks.Select(Database.Tasks.Create).Merge();

            if (databaseState.TimeEntries.Count > 0)
                await databaseState.TimeEntries.Select(Database.TimeEntries.Create).Merge();

            databaseState.SinceParameters?.Keys.ForEach(modelType =>
            {
                var sinceValue = databaseState.SinceParameters[modelType];
                var setMethod = typeof(ISinceParameterRepository).GetMethod(nameof(ISinceParameterRepository.Set));
                var genericSetMethod = setMethod.MakeGenericMethod(modelType);
                genericSetMethod.Invoke(
                    Database.SinceParameters, new object[] { sinceValue });
            });
        }

        public Task Clear()
            => Database.Clear().ToTask();
    }
}
