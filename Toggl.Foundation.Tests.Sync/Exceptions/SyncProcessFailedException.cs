using System;

namespace Toggl.Foundation.Tests.Sync.Exceptions
{
    public sealed class SyncProcessFailedException : Exception
    {
        private const string defaultInfoMessage =
            "If this is the desired output, override the `Act` method of the test " +
            "and handle syncing progress failures yourself.";

        public SyncProcessFailedException(string message)
            : base($"{message} {defaultInfoMessage}")
        {
        }
    }
}
