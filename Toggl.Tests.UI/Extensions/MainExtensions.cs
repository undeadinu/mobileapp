using System;
using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static class MainExtensions
    {
        public static void WaitForMainScreen(this IApp app)
        {
            var email = $"{Guid.NewGuid().ToString()}@toggl.space";

            app.WaitForSignUpScreen();

            app.Tap(SignUp.EmailText);
            app.EnterText(email);
            app.Tap(SignUp.PasswordText);
            app.EnterText("123456");
            app.SignUpSuccesfully();
        }

        public static void StartTimeEntryWithDescription(this IApp app, string description)
        {
            app.Tap(Main.StartTimeEntryButton);
            app.WaitForElement(StartTimeEntry.DoneButton);

            app.EnterText(description);
            app.Tap(StartTimeEntry.DoneButton);

            app.WaitForElement(Main.StopTimeEntryButton);
        }
    }
}
