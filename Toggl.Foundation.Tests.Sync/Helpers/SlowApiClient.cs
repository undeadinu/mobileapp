using System;
using System.Threading.Tasks;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Tests.Sync.Helpers
{
    internal sealed class SlowApiClient : IApiClient
    {
        private readonly TimeSpan delayBeforeRequest = TimeSpan.FromMilliseconds(100);

        private readonly IApiClient internalApiClient;

        public SlowApiClient(IApiClient internalApiClient)
        {
            this.internalApiClient = internalApiClient;
        }

        public void Dispose()
        {
            internalApiClient.Dispose();
        }

        public async Task<IResponse> Send(IRequest request)
        {
            await Task.Delay(delayBeforeRequest);
            return await internalApiClient.Send(request);
        }
    }
}
