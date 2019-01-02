using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static class AppQueryExtensions
    {
        public static void WaitForElementWithText(this IApp app, string element, string text)
        {
            app.WaitForElement(x => x.Marked(element).Text(text));
        }
    }
}
