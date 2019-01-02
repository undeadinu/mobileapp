using System;
using Xamarin.UITest.Queries;

namespace Toggl.Tests.UI
{
    public static class Onboarding
    {
        public const string FirstOnboardingElement = "";
        public const string SkipButton = "";
        public const string NextButton = "";
        public const string FirstLabel = "";
        public const string SecondLabel = "";
        public const string ThirdLabel = "";
        public const string PreviousButton = "";
    }

    public static class Login
    {
        public const string EmailText = "LoginEmailEditText";
        public const string ErrorLabel = "LoginError";
        public const string LoginButton = "LoginLoginButton";
        public const string PasswordText = "LoginPasswordEditText";
        public const string ShowPasswordButton = "";
        public const string SwitchToSignUpLabel = "LoginSignupCardView";
        public const string ForgotPasswordButton = "LoginForgotPassword";
    }

    public static class SignUp
    {
        public const string EmailText = "SignUpEmail";
        public const string SignUpButton = "SignUpButton";
        public const string PasswordText = "SignUpPassword";
        public const string GdprButton = "AcceptButton";
        public const string GdprCancelButton = "";
        public const string ErrorLabel = "SignUpError";
    }

    public static class Main
    {
        public static readonly Func<AppQuery, AppQuery> StartTimeEntryButton = x => x.Id("MainPlayButton");
        public static readonly Func<AppQuery, AppQuery> StopTimeEntryButton = x => x.Id("MainStopButton");
    }

    public static class StartTimeEntry
    {
        public static readonly Func<AppQuery, AppQuery> DoneButton = x => x.Id("StartTimeEntryDoneButton");
        public static readonly Func<AppQuery, AppQuery> DescriptionText = x => x.Id("StartTimeEntryDescriptionTextField");
        public const string CloseButton = "StartTimeEntryClose";
        public const string DialogDiscard = "Discard";
        public const string DialogCancel = "Cancel";
    }

    public static class ForgotPassword
    {
        public const string EmailText = "LoginEmailEditText";
        public const string ErrorLabel = "LoginEmail";
        public const string GetLinkButton = "ResetPasswordButton";
        public const string DoneCard = "Link sent. Please, check your email to reset the password";
    }
}
