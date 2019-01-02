using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static partial class SignUpExtensions
    {
        public static void RejectTerms(this IApp app)
        {
            app.WaitForElement(SignUp.GdprButton);
            app.Back();
        }
    }
}
