using System.Collections.Generic;
using Toggl.Foundation.Exceptions;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Xunit;
using static Toggl.Multivac.Extensions.FunctionalExtensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectUserCalendarsViewModelBaseTests
    {
        public sealed class MockSelectUserCalendarsViewModel : SelectUserCalendarsViewModelBase
        {
            public MockSelectUserCalendarsViewModel(IUserPreferences userPreferences, IInteractorFactory interactorFactory, IRxActionFactory rxActionFactory)
                : base(userPreferences, interactorFactory, rxActionFactory)
            {
            }
        }

        public abstract class SelectUserCalendarsViewModelBaseTest : BaseViewModelTests<MockSelectUserCalendarsViewModel>
        {
            protected SelectUserCalendarsViewModelBaseTest()
            {
                UserPreferences.EnabledCalendarIds().Returns(new List<string>());
            }

            protected override MockSelectUserCalendarsViewModel CreateViewModel()
                => new MockSelectUserCalendarsViewModel(UserPreferences, InteractorFactory, RxActionFactory);
        }

        public sealed class TheInitializeMethod : SelectUserCalendarsViewModelBaseTest
        {
            [Fact, LogIfTooSlow]
            public async Task FillsTheCalendarList()
            {
                var userCalendarsObservable = Enumerable
                    .Range(0, 9)
                    .Select(id => new UserCalendar(
                        id.ToString(),
                        $"Calendar #{id}",
                        $"Source #{id % 3}",
                        false))
                    .Apply(Observable.Return);
                InteractorFactory.GetUserCalendars().Execute().Returns(userCalendarsObservable);

                await ViewModel.Initialize();

                ViewModel.Calendars.Should().HaveCount(3);
                ViewModel.Calendars.ForEach(group => group.Should().HaveCount(3));
            }

            [Fact, LogIfTooSlow]
            public async Task HandlesNotAuthorizedException()
            {
                InteractorFactory
                    .GetUserCalendars()
                    .Execute()
                    .Returns(Observable.Throw<IEnumerable<UserCalendar>>(new NotAuthorizedException("")));

                await ViewModel.Initialize();

                ViewModel.Calendars.Should().HaveCount(0);
            }

            [Fact, LogIfTooSlow]
            public async Task MarksAllCalendarsAsNotSelected()
            {
                var userCalendarsObservable = Enumerable
                    .Range(0, 9)
                    .Select(id => new UserCalendar(
                        id.ToString(),
                        $"Calendar #{id}",
                        $"Source #{id % 3}",
                        false))
                    .Apply(Observable.Return);
                InteractorFactory.GetUserCalendars().Execute().Returns(userCalendarsObservable);

                await ViewModel.Initialize();

                foreach (var calendarGroup in ViewModel.Calendars)
                {
                    calendarGroup.All(calendar => !calendar.Selected);
                }
            }
        }
    }
}
