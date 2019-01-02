using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static partial class SignUpExtensions
    {
        public static void WaitForSignUpScreen(this IApp app)
        {
            app.WaitForLoginScreen();

            app.Tap(Login.SwitchToSignUpLabel);
            app.WaitForElement(SignUp.SignUpButton);
        }

        public static void TrySigningUpAndFail(this IApp app)
        {
            app.WaitForDefaultCountryToBeAutoSelected();
            app.Tap(SignUp.SignUpButton);
            app.WaitForElement(Login.ErrorLabel);
        }

        public static void SignUpSuccesfully(this IApp app)
        {
            app.WaitForDefaultCountryToBeAutoSelected();

            app.Tap(SignUp.SignUpButton);
            app.WaitForElement(SignUp.GdprButton);

            app.Tap(SignUp.GdprButton);
            app.WaitForElement(Main.StartTimeEntryButton);
        }

        public static void WaitForDefaultCountryToBeAutoSelected(this IApp app)
        {
            app.WaitForNoElement("Select country...");
        }
    }
}
