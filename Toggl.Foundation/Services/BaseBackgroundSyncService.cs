using System;
using System.Reactive.Linq;
using Toggl.Foundation.Login;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Services
{
    public abstract class BaseBackgroundSyncService : IBackgroundSyncService, IDisposable
    {
        private IDisposable loggedInDisposable;
        private IDisposable loggedOutDisposable;

        protected static readonly TimeSpan MinimumBackgroundFetchInterval = TimeSpan.FromMinutes(15);

        public void SetupBackgroundSync(IUserAccessManager loginManager)
        {
            loggedInDisposable = loginManager.UserLoggedIn
                .Do(EnableBackgroundSync)
                .Subscribe();

            loggedOutDisposable = loginManager.UserLoggedOut
                .Do(DisableBackgroundSync)
                .Subscribe();
        }

        public abstract void EnableBackgroundSync();
        public abstract void DisableBackgroundSync();

        public void Dispose()
        {
            loggedInDisposable.Dispose();
            loggedOutDisposable.Dispose();
        }
    }
}
