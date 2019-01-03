using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Models;

namespace Toggl.Foundation.Tests.Sync.Extensions
{
    public static class IProjectExtensions
    {
        public static IProject With(
            this IProject project,
            New<long> workspaceId = default(New<long>),
            New<long?> clientId = default(New<long?>))
            => new Project
            {
                Id = project.Id,
                ServerDeletedAt = project.ServerDeletedAt,
                At = project.At,
                WorkspaceId = workspaceId.ValueOr(project.WorkspaceId),
                ClientId = clientId.ValueOr(project.ClientId),
                Name = project.Name,
                IsPrivate = project.IsPrivate,
                Active = project.Active,
                Color = project.Color,
                Billable = project.Billable,
                Template = project.Template,
                AutoEstimates = project.AutoEstimates,
                EstimatedHours = project.EstimatedHours,
                Rate = project.Rate,
                Currency = project.Currency,
                ActualHours = project.ActualHours
            };
    }
}
