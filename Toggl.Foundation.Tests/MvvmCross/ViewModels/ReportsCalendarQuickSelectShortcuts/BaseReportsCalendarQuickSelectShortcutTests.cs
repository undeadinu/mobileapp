using System;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels.ReportsCalendarQuickSelectShortcuts
{
    public abstract class BaseReportsCalendarQuickSelectShortcutTests<T> : BaseMvvmCrossTests
        where T : ReportsCalendarBaseQuickSelectShortcut
    {
        protected BaseReportsCalendarQuickSelectShortcutTests()
        {
            TimeService.CurrentDateTime.Returns(CurrentTime);
        }

        protected abstract T CreateQuickSelectShortcut();
        protected abstract T TryToCreateQuickSelectShortCutWithNull();

        protected abstract DateTimeOffset CurrentTime { get; }
        protected abstract DateTime ExpectedStart { get; }
        protected abstract DateTime ExpectedEnd { get; }

        [Fact, LogIfTooSlow]
        public void SetsSelectedToTrueWhenReceivesOnDateRangeChangedWithOwnDateRange()
        {
            var quickSelectShortCut = CreateQuickSelectShortcut();
            var dateRange = quickSelectShortCut.GetDateRange();

            var result = quickSelectShortCut.IsSelected(dateRange);

            result.Should().BeTrue();
        }

        [Fact, LogIfTooSlow]
        public void TheGetDateRangeReturnsExpectedDateRange()
        {
            var dateRange = CreateQuickSelectShortcut().GetDateRange();

            dateRange.StartDate.Date.Should().Be(ExpectedStart);
            dateRange.EndDate.Date.Should().Be(ExpectedEnd);
        }

        [Fact, LogIfTooSlow]
        public void ConstructorThrowsWhenTryingToConstructWithNull()
        {
            Action tryingToConstructWithNull =
                () => TryToCreateQuickSelectShortCutWithNull();

            tryingToConstructWithNull.Should().Throw<ArgumentNullException>();
        }
    }
}
