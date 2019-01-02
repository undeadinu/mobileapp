using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Xamarin.UITest;
using Toggl.Tests.UI.Helpers;
using static Toggl.Tests.UI.Extensions.LoginExtensions;

namespace Toggl.Tests.UI
{
    [TestFixture]
    public sealed class LoginTests
    {
        private const string validEmail = "susancalvin@psychohistorian.museum";

        private IApp app;

        [SetUp]
        public void BeforeEachTest()
        {
            app = Configuration.GetApp();

            app.WaitForLoginScreen();
        }

        [Test]
        public void TheLoginButtonAfterInputtingAnInvalidCredentialsShowsTheErrorLabel()
        {
            var email = randomEmail();
            var password = "asdads";

            app.Tap(Login.EmailText);
            app.EnterText(email);
            app.Tap(Login.PasswordText);
            app.EnterText($"{password}123456");
            app.TryLoginAndFail();

            app.Screenshot("Login email page.");
        }

        [Test]
        public async Task TheLoginButtonAfterInputtingAValidCredentialsShowTheMainScreen()
        {
            var email = randomEmail();
            var password = await User.Create(email);

            app.Tap(Login.EmailText);
            app.EnterText(email);
            app.Tap(Login.PasswordText);
            app.EnterText(password);
            app.LoginSuccesfully();

            app.Screenshot("Login email page.");
        }

        private string randomEmail()
            => $"{Guid.NewGuid().ToString()}@toggl.space";
    }
}
