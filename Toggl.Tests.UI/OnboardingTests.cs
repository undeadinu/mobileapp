using NUnit.Framework;
using Xamarin.UITest;
using static Toggl.Tests.UI.Extensions.OnboardingExtensions;

namespace Toggl.Tests.UI
{
    [TestFixture]
    [IgnoreOnAndroid]
    public sealed class OnboardingTests
    {
        private IApp app;

        [SetUp]
        public void BeforeEachTest()
        {
            app = Configuration.GetApp();

            app.WaitForOnboardingScreen();
        }

        [Test]
        public void FirstScreenIsTheOnboardingScreen()
        {
            app.Screenshot("Onboarding screen.");
        }

        [Test]
        public void ClickingTheNextButtonShowsTheSecondPage()
        {
            app.GoForwardToSecondOnboardingPage();

            app.Screenshot("Onboarding second page.");
        }

        [Test]
        public void ClickingTheNextTwiceButtonShowsTheThirdPage()
        {
            app.GoForwardToSecondOnboardingPage();
            app.GoForwardToThirdOnboardingPage();

            app.Screenshot("Onboarding third page.");
        }

        [Test]
        public void ClickingTheNextButtonThriceShowsTheLoginScreen()
        {
            app.GoForwardToSecondOnboardingPage();
            app.GoForwardToThirdOnboardingPage();
            app.GoForwardToLoginPage();

            app.Screenshot("Login page.");
        }

        [Test]
        public void SwippingRightOnTheFirstLabelShowsTheSecondPage()
        {
            app.SwipeForwardToSecondOnboardingPage();

            app.Screenshot("Onboarding second page.");
        }

        [Test]
        public void SwippingRightOnTheSecondLabelShowsTheThirdPage()
        {
            app.SwipeForwardToSecondOnboardingPage();
            app.SwipeForwardToThirdOnboardingPage();

            app.Screenshot("Onboarding third page.");
        }

        [Test]
        public void TheBackButtonOnTheSecondPageShowsTheFirstPageAgain()
        {
            app.GoForwardToSecondOnboardingPage();
            app.GoBackToFirstOnboardingPage();

            app.Screenshot("Onboarding first page.");
        }

        [Test]
        public void TheBackButtonOnTheThirdPageShowsTheSecondPageAgain()
        {
            app.GoForwardToSecondOnboardingPage();
            app.GoForwardToThirdOnboardingPage();
            app.GoBackToSecondOnboardingPage();

            app.Screenshot("Onboarding second page.");
        }

        [Test]
        public void SwipingLeftOnTheSecondLabelShowsTheFirstPage()
        {
            app.SwipeForwardToSecondOnboardingPage();
            app.SwipeBackToFirstOnboardingPage();

            app.Screenshot("Onboarding first page.");
        }

        [Test]
        public void SwipingLeftOnTheThirdLabelShowsTheSecondPage()
        {
            app.SwipeForwardToSecondOnboardingPage();
            app.SwipeForwardToThirdOnboardingPage();
            app.SwipeBackToSecondOnboardingPage();

            app.Screenshot("Onboarding second page.");
        }

        [Test]
        public void ClickingTheSkipButtonGoesDirectlyToTheLoginPage()
        {
            app.SkipToLoginPage();

            app.Screenshot("Login page.");
        }
    }
}
