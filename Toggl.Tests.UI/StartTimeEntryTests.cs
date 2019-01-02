using NUnit.Framework;
using Xamarin.UITest;
using static Toggl.Tests.UI.Extensions.StartTimeEntryExtensions;

namespace Toggl.Tests.UI
{
    [TestFixture]
    public sealed class StartTimeEntryTests
    {
        private const string validEmail = "susancalvin@psychohistorian.museum";

        private IApp app;

        [SetUp]
        public void BeforeEachTest()
        {
            app = Configuration.GetApp();

            app.WaitForStartTimeEntryScreen();
        }

        [Test]
        public void TappingTheDoneButtonCreatesANewTimeEntry()
        {
            app.Tap(StartTimeEntry.DoneButton);

            app.WaitForElement(Main.StopTimeEntryButton);
        }

        [Test]
        public void TappingTheDoneButtonCreatesANewTimeEntryWhoseDescriptionMatchesWhatWasTypedInTheDescriptionField()
        {
            var description = "UI testing the Toggl App";

            app.EnterText(description);
            app.Tap(StartTimeEntry.DoneButton);

            app.WaitForElement(query => query.Marked(description));
        }

        [Test]
        public void TappingTheCloseButtonShowsConfirmationDialog()
        {
            var description = "UI testing the Toggl App";

            app.EnterText(description);
            app.Tap(StartTimeEntry.CloseButton);

            app.WaitForElement(StartTimeEntry.DialogDiscard);
        }

        [Test]
        public void TappingDiscardWhenClosingShouldNotStartATimeEntry()
        {
            var description = "UI testing the Toggl App";

            app.EnterText(description);
            app.Tap(StartTimeEntry.CloseButton);
            app.Tap(StartTimeEntry.DialogDiscard);

            app.WaitForNoElement(Main.StopTimeEntryButton);
        }

        [Test]
        public void TappingDoneAfterTryingToDiscardAndEditingTheDescriptionDoesNotChangeTheCurrentDescription()
        {
            var description = "UI testing the Toggl App";
            var edit = " - edit";

            app.EnterText(description);
            app.Tap(StartTimeEntry.CloseButton);
            app.Tap(StartTimeEntry.DialogCancel);

            app.EnterText(edit);
            app.Tap(StartTimeEntry.DoneButton);

            app.WaitForElement(query => query.Marked(description + edit));
        }
    }
}
