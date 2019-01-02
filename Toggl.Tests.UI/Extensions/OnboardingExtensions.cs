using System;
using System.Linq;
using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static class OnboardingExtensions
    {
        public static void WaitForOnboardingScreen(this IApp app)
            => app.WaitForElement(Onboarding.FirstOnboardingElement);

        public static void GoForwardToSecondOnboardingPage(this IApp app)
        {
            app.Tap(Onboarding.NextButton);
            app.WaitForElement(Onboarding.SecondLabel);
        }

        public static void GoForwardToThirdOnboardingPage(this IApp app)
        {
            app.Tap(Onboarding.NextButton);
            app.WaitForElement(Onboarding.ThirdLabel);
        }

        public static void GoForwardToLoginPage(this IApp app)
        {
            app.Tap(Onboarding.NextButton);
            app.WaitForElement(Login.LoginButton);
        }

        public static void SwipeForwardToSecondOnboardingPage(this IApp app)
        {
            app.SwipeRightToLeft(Onboarding.FirstLabel);
            app.WaitForElement(Onboarding.SecondLabel);
        }

        public static void SwipeForwardToThirdOnboardingPage(this IApp app)
        {
            app.SwipeRightToLeft(Onboarding.SecondLabel);
            app.WaitForElement(Onboarding.ThirdLabel);
        }

        public static void GoBackToFirstOnboardingPage(this IApp app)
        {
            app.Tap(Onboarding.PreviousButton);
            app.WaitForElement(Onboarding.FirstLabel);
        }

        public static void GoBackToSecondOnboardingPage(this IApp app)
        {
            app.Tap(Onboarding.PreviousButton);
            app.WaitForElement(Onboarding.SecondLabel);
        }

        public static void SwipeBackToFirstOnboardingPage(this IApp app)
        {
            app.SwipeLeftToRight(Onboarding.SecondLabel);
            app.WaitForElement(Onboarding.FirstLabel);
        }

        public static void SwipeBackToSecondOnboardingPage(this IApp app)
        {
            app.SwipeLeftToRight(Onboarding.ThirdLabel);
            app.WaitForElement(Onboarding.SecondLabel);
        }

        public static void SkipToLoginPage(this IApp app)
        {
            var skip = app.Query(Onboarding.SkipButton);
            if (skip != null && skip.Any())
            {
                app.Tap(Onboarding.SkipButton);
            }
            app.WaitForElement(Login.LoginButton);
        }
    }
}
