using System;
using Toggl.Foundation.Sync;
using Toggl.Multivac;

namespace Toggl.Foundation.Services
{
    public sealed class SyncErrorHandlingService : ISyncErrorHandlingService
    {
        private readonly IErrorHandlingService errorHandlingService;

        private IDisposable subscription;

        public SyncErrorHandlingService(IErrorHandlingService errorHandlingService)
        {
            Ensure.Argument.IsNotNull(errorHandlingService, nameof(errorHandlingService));

            this.errorHandlingService = errorHandlingService;
        }

        public void HandleErrorsOf(ISyncManager syncManager)
        {
            subscription?.Dispose();
            subscription = syncManager.Errors.Subscribe(onError);
        }

        private void onError(Exception exception)
        {
            if (!errorHandlingService.TryHandleDeprecationError(exception)
                && !errorHandlingService.TryHandleUnauthorizedError(exception)
                && !errorHandlingService.TryHandleNoWorkspaceError(exception)
                && !errorHandlingService.TryHandleNoDefaultWorkspaceError(exception))
            {
                throw new ArgumentException(
                    $"{nameof(SyncErrorHandlingService)} could not handle unknown sync error {exception.GetType().FullName}.",
                    exception);
            }
        }
    }
}
