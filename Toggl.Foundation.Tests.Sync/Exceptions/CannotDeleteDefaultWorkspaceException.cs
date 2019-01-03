using System;

namespace Toggl.Foundation.Tests.Sync.Exceptions
{
    public class CannotDeleteDefaultWorkspaceException : Exception
    {
        private const string defaultMessage =
            "It was not possible to delete the default workspace although it was missing in the desired server state.";

        public CannotDeleteDefaultWorkspaceException(Exception apiException)
            : base(defaultMessage, apiException)
        {
        }
    }
}
