using System;
using System.Linq;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class TimeEntryViewModel : ITimeEntryPrototype, IEquatable<TimeEntryViewModel>
    {
        public long Id { get; }

        public long WorkspaceId { get; }

        public bool IsBillable { get; }

        public string Description { get; } = "";

        public long? ProjectId { get; }

        public string ProjectName { get; } = "";

        public string ProjectColor { get; } = "#00000000";

        public string ClientName { get; } = "";

        public long? TaskId { get; }

        public string TaskName { get; } = "";

        public DateTimeOffset StartTime { get; }

        public TimeSpan? Duration { get; }

        public bool HasProject { get; }

        public bool HasTags { get; }

        public bool HasDescription { get; }

        public bool NeedsSync { get; }

        public bool CanSync { get; }

        public long[] TagIds { get; }

        public DurationFormat DurationFormat { get; }

        public bool IsInaccessible { get; }

        public bool CanContinue => CanSync && !IsInaccessible;

        public TimeEntryViewModel(IThreadSafeTimeEntry timeEntry, DurationFormat durationFormat)
        {
            Ensure.Argument.IsNotNull(timeEntry, nameof(timeEntry));

            if (timeEntry.IsRunning())
                throw new InvalidOperationException("It is not possible to show a running time entry in the log.");

            DurationFormat = durationFormat;

            Id = timeEntry.Id;
            WorkspaceId = timeEntry.WorkspaceId;
            StartTime = timeEntry.Start;
            IsBillable = timeEntry.Billable;
            TagIds = timeEntry.TagIds.ToArray();
            HasTags = TagIds.Count() > 0;
            Description = timeEntry.Description;
            HasProject = timeEntry.Project != null;
            Duration = TimeSpan.FromSeconds(timeEntry.Duration.Value);
            HasDescription = !string.IsNullOrEmpty(timeEntry.Description);

            CanSync = timeEntry.SyncStatus != SyncStatus.SyncFailed;
            NeedsSync = timeEntry.SyncStatus == SyncStatus.SyncNeeded;

            IsInaccessible = timeEntry.IsInaccessible;

            if (!HasProject) return;

            ProjectId = timeEntry.Project.Id;
            ProjectName = timeEntry.Project.DisplayName();
            ProjectColor = timeEntry.Project.DisplayColor();

            TaskId = timeEntry.TaskId;
            TaskName = timeEntry.Task?.Name ?? "";

            ClientName = timeEntry.Project.Client?.Name ?? "";
        }

        public bool Equals(TimeEntryViewModel other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id
                   && IsBillable == other.IsBillable
                   && string.Equals(Description, other.Description)
                   && string.Equals(ProjectName, other.ProjectName)
                   && string.Equals(ProjectColor, other.ProjectColor)
                   && string.Equals(ClientName, other.ClientName)
                   && string.Equals(TaskName, other.TaskName)
                   && Duration.Equals(other.Duration)
                   && HasProject == other.HasProject
                   && HasTags == other.HasTags
                   && NeedsSync == other.NeedsSync
                   && CanSync == other.CanSync
                   && IsInaccessible == other.IsInaccessible;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            return obj is TimeEntryViewModel other && Equals(other);
        }

        public override int GetHashCode()
            => HashCode.From(
                Id,
                IsBillable,
                Description ?? default(string),
                ProjectName ?? default(string),
                ProjectColor ?? default(string),
                ClientName ?? default(string),
                TaskName ?? default(string),
                Duration,
                HasProject,
                HasTags,
                NeedsSync,
                CanSync,
                IsInaccessible);

        public static bool operator ==(TimeEntryViewModel left, TimeEntryViewModel right) => Equals(left, right);

        public static bool operator !=(TimeEntryViewModel left, TimeEntryViewModel right) => !Equals(left, right);
    }
}
