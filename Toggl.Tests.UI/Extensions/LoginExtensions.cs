using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static partial class LoginExtensions
    {
        public static void TryLoginAndFail(this IApp app)
        {
            app.Tap(Login.LoginButton);

            app.WaitForElement(Login.ErrorLabel);
        }

        public static void LoginSuccesfully(this IApp app)
        {
            app.Tap(Login.LoginButton);

            app.WaitForElement(Main.StartTimeEntryButton);
        }
    }
}
