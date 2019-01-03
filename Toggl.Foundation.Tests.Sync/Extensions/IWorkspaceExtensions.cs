using System.Collections.Generic;
using System.Linq;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Toggl.Multivac.Models;

namespace Toggl.Foundation.Tests.Sync.Extensions
{
    public static class IWorkspaceExtensions
    {
        public static IThreadSafeWorkspace ToSyncable(
            this IWorkspace workspace)
            => new MockWorkspace
            {
                Admin = workspace.Admin,
                At = workspace.At,
                DefaultCurrency = workspace.DefaultCurrency,
                DefaultHourlyRate = workspace.DefaultHourlyRate,
                Id = workspace.Id,
                IsDeleted = false,
                IsInaccessible = false,
                LastSyncErrorMessage = null,
                LogoUrl = workspace.LogoUrl,
                Name = workspace.Name,
            };

        public static IEnumerable<IThreadSafeWorkspace> ToSyncable(this IEnumerable<IWorkspace> workspaces)
            => workspaces.Select(workspace => workspace.ToSyncable());

        public static IWorkspace With(
            this IWorkspace workspace,
            New<string> name = default(New<string>))
            => new Ultrawave.Models.Workspace
            {
                Admin = workspace.Admin,
                At = workspace.At,
                DefaultCurrency = workspace.DefaultCurrency,
                DefaultHourlyRate = workspace.DefaultHourlyRate,
                Id = workspace.Id,
                LogoUrl = workspace.LogoUrl,
                Name = name.ValueOr(workspace.Name)
            };
    }
}
