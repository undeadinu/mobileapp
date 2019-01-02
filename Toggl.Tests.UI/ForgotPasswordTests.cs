using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Toggl.Tests.UI.Extensions;
using Toggl.Tests.UI.Helpers;
using Xamarin.UITest;

namespace Toggl.Tests.UI
{
    [TestFixture]
    public sealed class ForgotPasswordTests
    {
        private const string invalidEmailFormat = "susancalvin@psycho";

        private IApp app;

        [SetUp]
        public void BeforeEachTest()
        {
            app = Configuration.GetApp();

            app.WaitForLoginScreen();
            app.Tap(Login.ForgotPasswordButton);
        }

        [Test]
        public void TheGetPasswordResetLinkButtonIsDisabledAfterEnterAnInvalidEmailFormat()
        {
            app.Tap(ForgotPassword.EmailText);
            app.EnterText(invalidEmailFormat);
            var getLinkButton = app.WaitForElement(ForgotPassword.GetLinkButton).First();
            Assert.False(getLinkButton.Enabled);
        }

        [Test]
        public void TheGetPasswordResetLinkButtonAfterEnterAnInvalidEmailShowsAnError()
        {
            var email = randomEmail();

            app.Tap(ForgotPassword.EmailText);
            app.EnterText(email);
            app.Tap(ForgotPassword.GetLinkButton);

            app.WaitForElementWithText(ForgotPassword.ErrorLabel, "Oops! Unknown email. Please check that it's entered correctly.");
        }

        [Test]
        public async Task TheGetPasswordResetLinkButtonAfterEnterAnValidEmailShowsTheDoneCard()
        {
            var email = randomEmail();
            await User.Create(email);

            app.Tap(ForgotPassword.EmailText);
            app.EnterText(email);
            app.Tap(ForgotPassword.GetLinkButton);
            app.WaitForElement(ForgotPassword.DoneCard);
        }

        private string randomEmail()
            => $"{Guid.NewGuid().ToString()}@toggl.space";
    }
}
