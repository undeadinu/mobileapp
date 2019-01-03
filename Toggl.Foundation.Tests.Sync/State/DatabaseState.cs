using System;
using System.Collections.Generic;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Mocks;

namespace Toggl.Foundation.Tests.Sync.State
{
    public struct DatabaseState
    {
        public IThreadSafeUser User { get; }
        public ISet<IThreadSafeClient> Clients { get; }
        public ISet<IThreadSafeProject> Projects { get; }
        public IThreadSafePreferences Preferences { get; }
        public ISet<IThreadSafeTag> Tags { get; }
        public ISet<IThreadSafeTask> Tasks { get; }
        public ISet<IThreadSafeTimeEntry> TimeEntries { get; }
        public ISet<IThreadSafeWorkspace> Workspaces { get; }
        public IDictionary<Type, DateTimeOffset?> SinceParameters { get; }

        public DatabaseState(
            IThreadSafeUser user,
            IEnumerable<IThreadSafeClient> clients = null,
            IEnumerable<IThreadSafeProject> projects = null,
            IThreadSafePreferences preferences = null,
            IEnumerable<IThreadSafeTag> tags = null,
            IEnumerable<IThreadSafeTask> tasks = null,
            IEnumerable<IThreadSafeTimeEntry> timeEntries = null,
            IEnumerable<IThreadSafeWorkspace> workspaces = null,
            IDictionary<Type, DateTimeOffset?> sinceParameters = null)
        {
            User = user;
            Clients = new HashSet<IThreadSafeClient>(clients ?? new IThreadSafeClient[0]);
            Projects = new HashSet<IThreadSafeProject>(projects ?? new IThreadSafeProject[0]);
            Preferences = preferences ?? new MockPreferences();
            Tags = new HashSet<IThreadSafeTag>(tags ?? new IThreadSafeTag[0]);
            Tasks = new HashSet<IThreadSafeTask>(tasks ?? new IThreadSafeTask[0]);
            TimeEntries = new HashSet<IThreadSafeTimeEntry>(timeEntries ?? new IThreadSafeTimeEntry[0]);
            Workspaces = new HashSet<IThreadSafeWorkspace>(workspaces ?? new IThreadSafeWorkspace[0]);
            SinceParameters = sinceParameters ?? new Dictionary<Type, DateTimeOffset?>();
        }
    }
}
