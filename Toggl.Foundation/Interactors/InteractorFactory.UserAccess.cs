using System;
using System.Reactive;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Interactors.UserAccess;

namespace Toggl.Foundation.Interactors
{
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        public IInteractor<IObservable<Unit>> Logout(LogoutSource source)
            => new LogoutInteractor(
                analyticsService,
                notificationService,
                shortcutCreator,
                syncManager,
                database,
                userPreferences,
                privateSharedStorageService,
                intentDonationService,
                userAccessManager,
                source);
    }
}
