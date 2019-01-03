using System.Collections.Generic;
using System.Linq;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Helpers;

namespace Toggl.Foundation.Tests.Sync.State
{
    public struct ServerState
    {
        public IUser User { get; }
        public ISet<IClient> Clients { get; }
        public ISet<IProject> Projects { get; }
        public IPreferences Preferences { get; }
        public ISet<ITag> Tags { get; }
        public ISet<ITask> Tasks { get; }
        public ISet<ITimeEntry> TimeEntries { get; }
        public ISet<IWorkspace> Workspaces { get; }
        public IDictionary<long, PricingPlans> PricingPlans { get; }

        public IWorkspace DefaultWorkspace
        {
            get
            {
                if (!User.DefaultWorkspaceId.HasValue)
                    return null;

                var defaultWorkspaceId = User.DefaultWorkspaceId.Value;
                return Workspaces.SingleOrDefault(workspace => workspace.Id == defaultWorkspaceId);
            }
        }

        public ServerState(
            IUser user,
            IEnumerable<IClient> clients = null,
            IEnumerable<IProject> projects = null,
            IPreferences preferences = null,
            IEnumerable<ITag> tags = null,
            IEnumerable<ITask> tasks = null,
            IEnumerable<ITimeEntry> timeEntries = null,
            IEnumerable<IWorkspace> workspaces = null,
            IDictionary<long, PricingPlans> pricingPlans = null)
        {
            User = user;
            Clients = new HashSet<IClient>(clients ?? new IClient[0]);
            Projects = new HashSet<IProject>(projects ?? new IProject[0]);
            Preferences = preferences ?? new MockPreferences();
            Tags = new HashSet<ITag>(tags ?? new ITag[0]);
            Tasks = new HashSet<ITask>(tasks ?? new ITask[0]);
            TimeEntries = new HashSet<ITimeEntry>(timeEntries ?? new ITimeEntry[0]);
            Workspaces = new HashSet<IWorkspace>(workspaces ?? new IWorkspace[0]);
            PricingPlans = pricingPlans ?? new Dictionary<long, PricingPlans>();
        }

        public ServerState With(
            New<IUser> user = default(New<IUser>),
            New<IEnumerable<IClient>> clients = default(New<IEnumerable<IClient>>),
            New<IEnumerable<IProject>> projects = default(New<IEnumerable<IProject>>),
            New<IPreferences> preferences = default(New<IPreferences>),
            New<IEnumerable<ITag>> tags = default(New<IEnumerable<ITag>>),
            New<IEnumerable<ITask>> tasks = default(New<IEnumerable<ITask>>),
            New<IEnumerable<ITimeEntry>> timeEntries = default(New<IEnumerable<ITimeEntry>>),
            New<IEnumerable<IWorkspace>> workspaces = default(New<IEnumerable<IWorkspace>>),
            New<IDictionary<long, PricingPlans>> pricingPlans = default(New<IDictionary<long, PricingPlans>>))
            => new ServerState(
                user.ValueOr(User),
                clients.ValueOr(Clients),
                projects.ValueOr(Projects),
                preferences.ValueOr(Preferences),
                tags.ValueOr(Tags),
                tasks.ValueOr(Tasks),
                timeEntries.ValueOr(TimeEntries),
                workspaces.ValueOr(Workspaces),
                pricingPlans.ValueOr(PricingPlans));
    }
}
