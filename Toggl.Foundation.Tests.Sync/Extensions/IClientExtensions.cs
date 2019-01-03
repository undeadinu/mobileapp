using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Models;

namespace Toggl.Foundation.Tests.Sync.Extensions
{
    public static class IClientExtensions
    {
        public static IClient With(
            this IClient client,
            New<long> workspaceId = default(New<long>))
            => new Client
            {
                Id = client.Id,
                ServerDeletedAt = client.ServerDeletedAt,
                At = client.At,
                Name = client.Name,
                WorkspaceId = workspaceId.ValueOr(client.WorkspaceId)
            };
    }
}
