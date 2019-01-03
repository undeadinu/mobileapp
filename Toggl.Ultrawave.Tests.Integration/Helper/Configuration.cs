using Toggl.Ultrawave.Network;

namespace Toggl.Ultrawave.Tests.Integration.Helper
{
    public static class Configuration
    {
        public static UserAgent UserAgent { get; }
            = new UserAgent("MobileIntegrationTests", "CAKE_COMMIT_HASH");
    }
}
