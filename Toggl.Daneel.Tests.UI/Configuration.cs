using System.Diagnostics;
using Xamarin.UITest;
using Xamarin.UITest.iOS;

namespace Toggl.Tests.UI
{
    public static class Configuration
    {
        private static string bootedDeviceId = null;

        public static iOSApp GetApp()
        {
            // FIXME:
            // For some unknown reason, UI tests in iOS are flaky.
            // Resetting the simulator solves this problem, but takes a lot of time.
            // A Xamarin.UITest update might fix this in the future.
            resetSimulator(bootedDeviceId);

            var app = ConfigureApp.iOS.StartApp();

            bootedDeviceId = app.Device.DeviceIdentifier;

            return app;
        }

        private static void resetSimulator(string deviceId)
        {
            if (deviceId == null)
                return;

            var shutdownCmdLine = "simctl shutdown all";
            var shutdownProcess = Process.Start("xcrun", shutdownCmdLine);
            shutdownProcess.WaitForExit();

            var startCmdLine = string.Format("simctl boot {0}", deviceId);
            var startProcess = Process.Start("xcrun", startCmdLine);
            startProcess.WaitForExit();
        }
    }
}
