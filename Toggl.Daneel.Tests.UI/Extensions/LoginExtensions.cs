using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static partial class LoginExtensions
    {
        public static void WaitForLoginScreen(this IApp app)
        {
            app.SkipToLoginPage();
        }
    }
}
