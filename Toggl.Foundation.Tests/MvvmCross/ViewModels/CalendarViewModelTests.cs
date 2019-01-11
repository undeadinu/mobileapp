using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Xunit;
using ITimeEntryPrototype = Toggl.Foundation.Models.ITimeEntryPrototype;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class CalendarViewModelTests
    {
        public abstract class CalendarViewModelTest : BaseViewModelTests<CalendarViewModel>
        {
            protected const long TimeEntryId = 10;
            protected const long DefaultWorkspaceId = 1;

            protected static DateTimeOffset Now { get; } = new DateTimeOffset(2018, 8, 10, 12, 0, 0, TimeSpan.Zero);

            protected IInteractor<IObservable<IEnumerable<CalendarItem>>> CalendarInteractor { get; }

            protected CalendarViewModelTest()
            {
                CalendarInteractor = Substitute.For<IInteractor<IObservable<IEnumerable<CalendarItem>>>>();

                var workspace = new MockWorkspace { Id = DefaultWorkspaceId };
                var timeEntry = new MockTimeEntry { Id = TimeEntryId };

                TimeService.CurrentDateTime.Returns(Now);

                InteractorFactory
                    .GetCalendarItemsForDate(Arg.Any<DateTime>())
                    .Returns(CalendarInteractor);

                InteractorFactory
                    .GetDefaultWorkspace()
                    .Execute()
                    .Returns(Observable.Return(workspace));

                InteractorFactory
                    .CreateTimeEntry(Arg.Any<ITimeEntryPrototype>())
                    .Execute()
                    .Returns(Observable.Return(timeEntry));

                InteractorFactory
                    .UpdateTimeEntry(Arg.Any<EditTimeEntryDto>())
                    .Execute()
                    .Returns(Observable.Return(timeEntry));
            }

            protected override CalendarViewModel CreateViewModel()
                => new CalendarViewModel(
                    DataSource,
                    TimeService,
                    DialogService,
                    UserPreferences,
                    AnalyticsService,
                    BackgroundService,
                    InteractorFactory,
                    OnboardingStorage,
                    SchedulerProvider,
                    PermissionsService,
                    NavigationService,
                    StopwatchProvider,
                    RxActionFactory
                );
        }

        public sealed class TheConstructor : CalendarViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useTimeService,
                bool useDialogService,
                bool useUserPreferences,
                bool useAnalyticsService,
                bool useBackgroundService,
                bool useInteractorFactory,
                bool useOnboardingStorage,
                bool useSchedulerProvider,
                bool useNavigationService,
                bool usePermissionsService,
                bool useStopwatchProvider,
                bool useRxActionFactory)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var dialogService = useDialogService ? DialogService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var backgroundService = useBackgroundService ? BackgroundService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var permissionsService = usePermissionsService ? PermissionsService : null;
                var stopwatchProvider = useStopwatchProvider ? StopwatchProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new CalendarViewModel(
                        dataSource,
                        timeService,
                        dialogService,
                        userPreferences,
                        analyticsService,
                        backgroundService,
                        interactorFactory,
                        onboardingStorage,
                        schedulerProvider,
                        permissionsService,
                        navigationService,
                        stopwatchProvider,
                        rxActionFactory);

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheInitMethod : CalendarViewModelTest
        {
            private const string eventId = "1337";

            private readonly CalendarItem calendarItem = new CalendarItem(
                eventId,
                CalendarItemSource.Calendar,
                new DateTimeOffset(2018, 08, 10, 0, 15, 0, TimeSpan.Zero),
                TimeSpan.FromMinutes(10),
                "Meeting with someone",
                CalendarIconKind.Event
            );

            public TheInitMethod()
            {
                InteractorFactory
                    .GetCalendarItemWithId(Arg.Is(eventId))
                    .Execute()
                    .Returns(Observable.Return(calendarItem));
            }

            [Theory, LogIfTooSlow]
            [InlineData("")]
            [InlineData(" ")]
            [InlineData("  ")]
            public void DoesNothingWithInvalidStrings(string invalidId)
            {
                ViewModel.Init(invalidId);

                InteractorFactory
                    .DidNotReceive()
                    .GetCalendarItemWithId(Arg.Any<string>());
            }

            [Fact, LogIfTooSlow]
            public void TracksTheAppropriateEventToTheAnalyticsService()
            {
                ViewModel.Init(eventId);

                AnalyticsService.TimeEntryStarted.Received().Track(TimeEntryStartOrigin.CalendarEvent);
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryUsingTheCalendarItemInfo()
            {
                ViewModel.Init(eventId);

                await InteractorFactory
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(p => p.Description == calendarItem.Description))
                    .Received()
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryInTheDefaultWorkspace()
            {
                ViewModel.Init(eventId);

                await InteractorFactory
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(p => p.WorkspaceId == DefaultWorkspaceId))
                    .Received()
                    .Execute();
            }
        }

        public sealed class TheShouldShowOnboardingProperty : CalendarViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsTrueIfCalendarOnboardingHasntBeenCompleted()
            {
                OnboardingStorage.CompletedCalendarOnboarding().Returns(false);
                var viewModel = CreateViewModel();
                var observer = TestScheduler.CreateObserver<bool>();

                viewModel.ShouldShowOnboarding.Subscribe(observer);

                TestScheduler.Start();
                observer.Messages.Single().Value.Value.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsFalseIfCalendarOnboardingHasBeenCompleted()
            {
                OnboardingStorage.CompletedCalendarOnboarding().Returns(true);
                var viewModel = CreateViewModel();
                var observer = TestScheduler.CreateObserver<bool>();

                viewModel.ShouldShowOnboarding.Subscribe(observer);
                TestScheduler.Start();

                observer.Messages.Single().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsFalseWhenUserGrantsCalendarAccess()
            {
                OnboardingStorage.CompletedCalendarOnboarding().Returns(false);
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.ShouldShowOnboarding.Subscribe(observer);
                PermissionsService.RequestCalendarAuthorization().Returns(Observable.Return(true));
                NavigationService.Navigate<SelectUserCalendarsViewModel, string[]>().Returns(new string[0]);

                ViewModel.GetStarted.Execute();
                TestScheduler.Start();

                observer.Messages.Select(message => message.Value.Value).AssertEqual(true, false);
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsFalseWhenUserWantsToContinueWithoutCalendarAccess()
            {
                OnboardingStorage.CompletedCalendarOnboarding().Returns(false);
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.ShouldShowOnboarding.Subscribe(observer);
                PermissionsService.RequestCalendarAuthorization().Returns(Observable.Return(false));
                NavigationService.Navigate<CalendarPermissionDeniedViewModel, Unit>().Returns(Unit.Default);

                ViewModel.GetStarted.Execute();
                TestScheduler.Start();

                observer.Messages.Select(message => message.Value.Value).AssertEqual(true, false);
            }
        }

        public sealed class TheSettingsAreVisibleObservable : CalendarViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task EmitsWheneverTheShouldShowOnboardingObservablesOmits()
            {
                PermissionsService.CalendarPermissionGranted.Returns(Observable.Return(false));
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.SettingsAreVisible.Subscribe(observer);
                PermissionsService.CalendarPermissionGranted.Returns(Observable.Return(true));
                ViewModel.GetStarted.Execute();

                TestScheduler.Start();

                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(0, false),
                    ReactiveTest.OnNext(0, true)
                );
            }
        }

        public sealed class TheSelectCalendarsAction : CalendarViewModelTest
        {
            protected override void AdditionalSetup()
            {
                OnboardingStorage
                    .CompletedCalendarOnboarding()
                    .Returns(true);

                PermissionsService
                    .CalendarPermissionGranted
                    .Returns(Observable.Return(true));

                NavigationService
                    .Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>())
                    .Returns(new string[0]);

                InteractorFactory
                    .GetUserCalendars()
                    .Execute()
                    .Returns(Observable.Return(new UserCalendar().Yield()));

                DialogService
                    .Alert(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                    .Returns(Observable.Return(Unit.Default));

                TestScheduler.AdvanceBy(1);
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheSelectUserCalendarsViewModelWhenThereAreCalendars()
            {
                ViewModel.SelectCalendars.Execute();
                TestScheduler.Start();

                await NavigationService
                    .Received()
                    .Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>());
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotNavigateToTheSelectUserCalendarsViewModelWhenThereAreNoCalendars()
            {
                InteractorFactory.GetUserCalendars().Execute().Returns(
                    Observable.Return(new UserCalendar[0])
                );

                ViewModel.SelectCalendars.Execute();
                TestScheduler.Start();

                await DialogService
                    .Received()
                    .Alert(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsADialogWhenThereAreNoCalendars()
            {
                InteractorFactory.GetUserCalendars().Execute().Returns(
                    Observable.Return(new UserCalendar[0])
                );

                ViewModel.SelectCalendars.Execute();
                TestScheduler.Start();

                await NavigationService.DidNotReceive().Navigate<SelectUserCalendarsViewModel, string[]>();
            }

            [Property]
            public void SetsTheEnabledCalendarsWhenThereAreCalendars(NonEmptyString[] nonEmptyStrings)
            {
                if (nonEmptyStrings == null) return;

                InteractorFactory.ClearReceivedCalls();
                var viewModel = CreateViewModel();

                var calendarIds = nonEmptyStrings.Select(str => str.Get).ToArray();
                NavigationService.Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>()).Returns(calendarIds);
                PermissionsService.RequestCalendarAuthorization().Returns(Observable.Return(true));
                InteractorFactory.GetUserCalendars().Execute().Returns(
                    Observable.Return(new UserCalendar[] { new UserCalendar() })
                );

                viewModel.SelectCalendars.Execute();
                TestScheduler.Start();

                InteractorFactory.Received().SetEnabledCalendars(calendarIds).Execute();
            }
        }

        public sealed class TheGetStartedAction : CalendarViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task RequestsCalendarPermission()
            {
                ViewModel.GetStarted.Execute();
                TestScheduler.Start();

                await PermissionsService.Received().RequestCalendarAuthorization();
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheCalendarPermissionDeniedViewModelWhenPermissionIsDenied()
            {
                PermissionsService.RequestCalendarAuthorization().Returns(Observable.Return(false));

                ViewModel.GetStarted.Execute();

                NavigationService.Received().Navigate<CalendarPermissionDeniedViewModel, Unit>();
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheSelectUserCalendarsViewModelWhenThereAreCalendars()
            {
                PermissionsService.RequestCalendarAuthorization().Returns(Observable.Return(true));
                InteractorFactory.GetUserCalendars().Execute().Returns(
                    Observable.Return(new UserCalendar[] { new UserCalendar() })
                );

                ViewModel.GetStarted.Execute();
                TestScheduler.Start();

                await NavigationService
                    .Received()
                    .Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>());
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotNavigateToTheSelectUserCalendarsViewModelWhenThereAreNoCalendars()
            {
                PermissionsService.RequestCalendarAuthorization().Returns(Observable.Return(true));
                InteractorFactory.GetUserCalendars().Execute().Returns(
                    Observable.Return(new UserCalendar[0])
                );

                ViewModel.GetStarted.Execute();
                TestScheduler.Start();

                await NavigationService.DidNotReceive().Navigate<SelectUserCalendarsViewModel, string[]>();
            }

            [Property]
            public void SetsTheEnabledCalendarsWhenThereAreCalendars(NonEmptyString[] nonEmptyStrings)
            {
                if (nonEmptyStrings == null) return;

                InteractorFactory.ClearReceivedCalls();
                var viewModel = CreateViewModel();

                var calendarIds = nonEmptyStrings.Select(str => str.Get).ToArray();
                NavigationService.Navigate<SelectUserCalendarsViewModel, bool, string[]>(Arg.Any<bool>()).Returns(calendarIds);
                PermissionsService.RequestCalendarAuthorization().Returns(Observable.Return(true));
                InteractorFactory.GetUserCalendars().Execute().Returns(
                    Observable.Return(new UserCalendar[] { new UserCalendar() })
                );

                viewModel.GetStarted.Execute(Unit.Default);
                TestScheduler.Start();

                InteractorFactory.Received().SetEnabledCalendars(calendarIds).Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsCalendarOnboardingAsCompletedIfUserGrantsAccess()
            {
                PermissionsService.RequestCalendarAuthorization().Returns(Observable.Return(true));
                NavigationService.Navigate<SelectUserCalendarsViewModel, string[]>().Returns(new string[0]);

                 ViewModel.GetStarted.Execute(Unit.Default);

                OnboardingStorage.Received().SetCompletedCalendarOnboarding();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsCalendarOnboardingAsCompletedIfUserWantsToContinueWithoutGivingPermission()
            {
                PermissionsService.RequestCalendarAuthorization().Returns(Observable.Return(false));
                NavigationService.Navigate<CalendarPermissionDeniedViewModel, Unit>().Returns(Unit.Default);

                ViewModel.GetStarted.Execute();
                TestScheduler.Start();

                OnboardingStorage.Received().SetCompletedCalendarOnboarding();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksTheCalendarOnbardingStartedEvent()
            {
                PermissionsService.RequestCalendarAuthorization().Returns(Observable.Return(false));

                ViewModel.GetStarted.Execute();
                TestScheduler.Start();

                AnalyticsService.CalendarOnboardingStarted.Received().Track();
            }

            [Fact, LogIfTooSlow]
            public async Task RequestsNotificationsPermissionIfCalendarPermissionWasGranted()
            {
                PermissionsService.RequestCalendarAuthorization().Returns(Observable.Return(true));
                NavigationService.Navigate<SelectUserCalendarsViewModel, string[]>().Returns(new string[0]);

                ViewModel.GetStarted.Execute(Unit.Default);
                TestScheduler.Start();

                await PermissionsService.Received().RequestNotificationAuthorization();
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async Task SetsTheNotificationPropertyAfterAskingForPermission(bool permissionWasGiven)
            {
                PermissionsService.RequestCalendarAuthorization().Returns(Observable.Return(true));
                NavigationService.Navigate<SelectUserCalendarsViewModel, string[]>().Returns(new string[0]);
                PermissionsService.RequestNotificationAuthorization().Returns(Observable.Return(permissionWasGiven));

                ViewModel.GetStarted.Execute();
                TestScheduler.Start();

                UserPreferences.Received().SetCalendarNotificationsEnabled(Arg.Is(permissionWasGiven));
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotRequestNotificationsPermissionIfCalendarPermissionWasNotGranted()
            {
                PermissionsService.RequestCalendarAuthorization().Returns(Observable.Return(false));
                NavigationService.Navigate<SelectUserCalendarsViewModel, string[]>().Returns(new string[0]);

                ViewModel.GetStarted.Execute();
                TestScheduler.Start();

                await PermissionsService.DidNotReceive().RequestNotificationAuthorization();
            }
        }

        public sealed class TheCalendarItemsProperty : CalendarViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ReturnsTheCalendarItemsForToday()
            {
                var now = new DateTimeOffset(2018, 8, 9, 12, 0, 0, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(now);

                var items = new List<CalendarItem>
                {
                    new CalendarItem("id", CalendarItemSource.Calendar, now.AddMinutes(30), TimeSpan.FromMinutes(15), "Weekly meeting", CalendarIconKind.Event, "#ff0000"),
                    new CalendarItem("id", CalendarItemSource.TimeEntry, now.AddHours(-3), TimeSpan.FromMinutes(30), "Bug fixes", CalendarIconKind.None, "#00ff00"),
                    new CalendarItem("id", CalendarItemSource.Calendar, now.AddHours(2), TimeSpan.FromMinutes(30), "F**** timesheets", CalendarIconKind.Event, "#ff0000")
                };
                var interactor = Substitute.For<IInteractor<IObservable<IEnumerable<CalendarItem>>>>();
                interactor.Execute().Returns(Observable.Return(items));
                InteractorFactory.GetCalendarItemsForDate(Arg.Any<DateTime>()).Returns(interactor);

                await ViewModel.Initialize();

                ViewModel.CalendarItems[0].Should().BeEquivalentTo(items);
            }

            [Fact, LogIfTooSlow]
            public async Task RefetchesWheneverATimeEntryIsAdded()
            {
                var deletedSubject = new Subject<long>();
                var midnightSubject = new Subject<DateTimeOffset>();
                var createdSubject = new Subject<IThreadSafeTimeEntry>();
                var updatedSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
                DataSource.TimeEntries.Deleted.Returns(deletedSubject);
                DataSource.TimeEntries.Updated.Returns(updatedSubject);
                DataSource.TimeEntries.Created.Returns(createdSubject);
                TimeService.MidnightObservable.Returns(midnightSubject);
                await ViewModel.Initialize();
                CalendarInteractor.ClearReceivedCalls();

                createdSubject.OnNext(new MockTimeEntry());

                await CalendarInteractor.Received().Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task RefetchesWheneverATimeEntryIsUpdated()
            {
                var deletedSubject = new Subject<long>();
                var midnightSubject = new Subject<DateTimeOffset>();
                var createdSubject = new Subject<IThreadSafeTimeEntry>();
                var updatedSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
                DataSource.TimeEntries.Deleted.Returns(deletedSubject);
                DataSource.TimeEntries.Updated.Returns(updatedSubject);
                DataSource.TimeEntries.Created.Returns(createdSubject);
                TimeService.MidnightObservable.Returns(midnightSubject);
                await ViewModel.Initialize();
                CalendarInteractor.ClearReceivedCalls();

                updatedSubject.OnNext(new EntityUpdate<IThreadSafeTimeEntry>(0, new MockTimeEntry()));

                await CalendarInteractor.Received().Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task RefetchesWheneverATimeEntryIsDeleted()
            {
                var deletedSubject = new Subject<long>();
                var midnightSubject = new Subject<DateTimeOffset>();
                var createdSubject = new Subject<IThreadSafeTimeEntry>();
                var updatedSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
                DataSource.TimeEntries.Deleted.Returns(deletedSubject);
                DataSource.TimeEntries.Updated.Returns(updatedSubject);
                DataSource.TimeEntries.Created.Returns(createdSubject);
                TimeService.MidnightObservable.Returns(midnightSubject);
                await ViewModel.Initialize();
                CalendarInteractor.ClearReceivedCalls();

                deletedSubject.OnNext(0);

                await CalendarInteractor.Received().Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task RefetchesWheneverTheDayChanges()
            {
                var deletedSubject = new Subject<long>();
                var midnightSubject = new Subject<DateTimeOffset>();
                var createdSubject = new Subject<IThreadSafeTimeEntry>();
                var updatedSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
                DataSource.TimeEntries.Deleted.Returns(deletedSubject);
                DataSource.TimeEntries.Updated.Returns(updatedSubject);
                DataSource.TimeEntries.Created.Returns(createdSubject);
                TimeService.MidnightObservable.Returns(midnightSubject);
                await ViewModel.Initialize();
                CalendarInteractor.ClearReceivedCalls();

                midnightSubject.OnNext(DateTimeOffset.Now);

                await CalendarInteractor.Received().Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task RefetchesWheneverTheSelectedCalendarsChange()
            {
                var deletedSubject = new Subject<long>();
                var calendarSubject = new Subject<List<string>>();
                var midnightSubject = new Subject<DateTimeOffset>();
                var createdSubject = new Subject<IThreadSafeTimeEntry>();
                var updatedSubject = new Subject<EntityUpdate<IThreadSafeTimeEntry>>();
                DataSource.TimeEntries.Deleted.Returns(deletedSubject);
                DataSource.TimeEntries.Updated.Returns(updatedSubject);
                DataSource.TimeEntries.Created.Returns(createdSubject);
                TimeService.MidnightObservable.Returns(midnightSubject);
                UserPreferences.EnabledCalendars.Returns(calendarSubject);
                await ViewModel.Initialize();
                CalendarInteractor.ClearReceivedCalls();

                calendarSubject.OnNext(new List<string>());

                await CalendarInteractor.Received().Execute();
            }
        }

        public abstract class TheOnItemTappedAction : CalendarViewModelTest
        {
            protected abstract void EnsureEventWasTracked();

            protected abstract CalendarItem CalendarItem { get; }

            [Fact, LogIfTooSlow]
            public async Task TracksTheAppropriateEventToTheAnalyticsService()
            {
                ViewModel.OnItemTapped.Execute(CalendarItem);
                TestScheduler.Start();

                EnsureEventWasTracked();
            }

            public sealed class WhenHandlingTimeEntryItems : TheOnItemTappedAction
            {
                protected override void EnsureEventWasTracked()
                {
                    AnalyticsService.EditViewOpenedFromCalendar.Received().Track();
                }

                protected override CalendarItem CalendarItem { get; } = new CalendarItem(
                    "id",
                    CalendarItemSource.TimeEntry,
                    new DateTimeOffset(2018, 08, 10, 0, 0, 0, TimeSpan.Zero),
                    TimeSpan.FromMinutes(10),
                    "Working on something",
                    CalendarIconKind.None,
                    "#00FF00",
                    TimeEntryId
                );

                [Fact]
                public async Task NavigatesToTheEditTimeEntryViewModelUsingTheTimeEntryId()
                {
                    ViewModel.OnItemTapped.Execute(CalendarItem);
                    TestScheduler.Start();

                    await NavigationService.Received().Navigate<EditTimeEntryViewModel, long>(Arg.Is(TimeEntryId));
                }
            }

            public sealed class WhenHandlingCalendarItems : TheOnItemTappedAction
            {
                protected override void EnsureEventWasTracked()
                {
                    AnalyticsService.TimeEntryStarted.Received().Track(TimeEntryStartOrigin.CalendarEvent);
                }

                protected override CalendarItem CalendarItem { get; } = new CalendarItem(
                    "id",
                    CalendarItemSource.Calendar,
                    new DateTimeOffset(2018, 08, 10, 0, 15, 0, TimeSpan.Zero),
                    TimeSpan.FromMinutes(10),
                    "Meeting with someone",
                    CalendarIconKind.Event
                );

                [Fact, LogIfTooSlow]
                public async Task CreatesATimeEntryUsingTheCalendarItemInfo()
                {
                    ViewModel.OnItemTapped.Execute(CalendarItem);
                    TestScheduler.Start();

                    await InteractorFactory
                        .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(p => p.Description == CalendarItem.Description))
                        .Received()
                        .Execute();
                }

                [Fact, LogIfTooSlow]
                public async Task CreatesATimeEntryInTheDefaultWorkspace()
                {
                    ViewModel.OnItemTapped.Execute(CalendarItem);
                    TestScheduler.Start();

                    await InteractorFactory
                        .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(p => p.WorkspaceId == DefaultWorkspaceId))
                        .Received()
                        .Execute();
                }
            }
        }

        public sealed class TheOnDurationSelectedAction : CalendarViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryWithTheSelectedStartDate()
            {
                var now = DateTimeOffset.UtcNow;
                var duration = TimeSpan.FromMinutes(30);
                var tuple = (now, duration);

                ViewModel.OnDurationSelected.Execute(tuple);
                TestScheduler.Start();

                await InteractorFactory
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(p => p.StartTime == now))
                    .Received()
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryWithTheSelectedDuration()
            {
                var now = DateTimeOffset.UtcNow;
                var duration = TimeSpan.FromMinutes(30);
                var tuple = (now, duration);

                ViewModel.OnDurationSelected.Execute(tuple);
                TestScheduler.Start();

                await InteractorFactory
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(p => p.Duration == duration))
                    .Received()
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesATimeEntryInTheDefaultWorkspace()
            {
                var now = DateTimeOffset.UtcNow;
                var duration = TimeSpan.FromMinutes(30);
                var tuple = (now, duration);

                ViewModel.OnDurationSelected.Execute(tuple);
                TestScheduler.Start();

                await InteractorFactory
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(p => p.WorkspaceId == DefaultWorkspaceId))
                    .Received()
                    .Execute();
            }

            [Fact]
            public async Task NavigatesToTheEditTimeEntryViewModelUsingTheTimeEntryId()
            {
                var now = DateTimeOffset.UtcNow;
                var duration = TimeSpan.FromMinutes(30);
                var tuple = (now, duration);

                ViewModel.OnDurationSelected.Execute(tuple);
                TestScheduler.Start();

                await NavigationService.Received().Navigate<EditTimeEntryViewModel, long>(Arg.Is(TimeEntryId));
            }

            [Fact, LogIfTooSlow]
            public async Task TracksTheTimeEntryCreatedFromCalendarTappingEventToTheAnalyticsService()
            {
                var now = DateTimeOffset.UtcNow;
                var duration = TimeSpan.FromMinutes(30);
                var tuple = (now, duration);

                ViewModel.OnDurationSelected.Execute(tuple);
                TestScheduler.Start();

                AnalyticsService.TimeEntryStarted.Received().Track(TimeEntryStartOrigin.CalendarTapAndDrag);
            }
        }

        public sealed class TheEditTimeEntryAction : CalendarViewModelTest
        {
            private CalendarItem calendarItem = new CalendarItem(
                "id",
                CalendarItemSource.TimeEntry,
                new DateTimeOffset(2018, 8, 20, 10, 0, 0, TimeSpan.Zero),
                new TimeSpan(45),
                "This is a time entry",
                CalendarIconKind.None,
                color: "#ff0000",
                timeEntryId: TimeEntryId,
                calendarId: "abcd-1234-abcd-1234");

            [Fact, LogIfTooSlow]
            public async Task UpdatesATimeEntry()
            {
                ViewModel.OnUpdateTimeEntry.Execute(calendarItem);

                await InteractorFactory
                    .UpdateTimeEntry(Arg.Is<DTOs.EditTimeEntryDto>(dto =>
                        dto.Id == TimeEntryId
                        && dto.StartTime == calendarItem.StartTime
                        && dto.StopTime == calendarItem.EndTime))
                    .Received()
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksTheTimeEntryEditedFromCalendarEventWhenDurationChanges()
            {
                var timeEntry = new MockTimeEntry
                {
                    Start = calendarItem.StartTime,
                    Duration = (long)calendarItem.Duration.Value.TotalSeconds + 10
                };
                InteractorFactory.GetTimeEntryById(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(timeEntry));

                ViewModel.OnUpdateTimeEntry.Execute(calendarItem);

                AnalyticsService.TimeEntryChangedFromCalendar.Received().Track(CalendarChangeEvent.Duration);
            }

            [Fact, LogIfTooSlow]
            public async Task TracksTheTimeEntryEditedFromCalendarEventWhenStartTimeChanges()
            {
                var timeEntry = new MockTimeEntry
                {
                    Start = calendarItem.StartTime.Add(TimeSpan.FromHours(1)),
                    Duration = (long)calendarItem.Duration.Value.TotalSeconds
                };
                InteractorFactory.GetTimeEntryById(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(timeEntry));

                ViewModel.OnUpdateTimeEntry.Execute(calendarItem);

                AnalyticsService.TimeEntryChangedFromCalendar.Received().Track(CalendarChangeEvent.StartTime);
            }

            [Fact, LogIfTooSlow]
            public async Task TracksTheTimeEntryEditedFromCalendarEventWhenBothStartTimeAndDurationChange()
            {
                var timeEntry = new MockTimeEntry
                {
                    Start = calendarItem.StartTime.Add(TimeSpan.FromHours(1)),
                    Duration = (long)calendarItem.Duration.Value.TotalSeconds + 10
                };
                InteractorFactory.GetTimeEntryById(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(timeEntry));

                ViewModel.OnUpdateTimeEntry.Execute(calendarItem);

                AnalyticsService.TimeEntryChangedFromCalendar.Received().Track(CalendarChangeEvent.Duration);
                AnalyticsService.TimeEntryChangedFromCalendar.Received().Track(CalendarChangeEvent.StartTime);
            }
        }

        public sealed class TheCalendarEventLongPressedAction : CalendarViewModelTest
        {
            private CalendarItem calendarEvent = new CalendarItem(
                "id",
                CalendarItemSource.Calendar,
                new DateTimeOffset(2018, 8, 20, 10, 0, 0, TimeSpan.Zero),
                new TimeSpan(45),
                "This is a calendar event",
                CalendarIconKind.None,
                color: "#ff0000",
                timeEntryId: TimeEntryId,
                calendarId: "abcd-1234-abcd-1234");

            [Fact, LogIfTooSlow]
            public async Task PresentsTwoOptionsToTheUserWhenTheEventStartsInTheFuture()
            {
                TimeService.CurrentDateTime.Returns(calendarEvent.StartTime - TimeSpan.FromHours(1));

                ViewModel.OnCalendarEventLongPressed.Inputs.OnNext(calendarEvent);

                await DialogService.Received().Select(
                    Arg.Any<string>(),
                    Arg.Is<IEnumerable<(string, CalendarItem?)>>(options => options.Count() == 2),
                    Arg.Any<int>());
            }

            [Fact, LogIfTooSlow]
            public async Task PresentsThreeOptionsToTheUserWhenTheEventStartsInThePast()
            {
                TimeService.CurrentDateTime.Returns(calendarEvent.StartTime + TimeSpan.FromHours(1));

                ViewModel.OnCalendarEventLongPressed.Inputs.OnNext(calendarEvent);

                await DialogService.Received().Select(
                    Arg.Any<string>(),
                    Arg.Is<IEnumerable<(string, CalendarItem?)>>(options => options.Count() == 3),
                    Arg.Any<int>());
            }

            [Fact, LogIfTooSlow]
            public void DoesNotCreateAnyTimeEntryWhenUserSelectsTheCancelOption()
            {
                DialogService.Select<CalendarItem?>(null, null, 0)
                    .ReturnsForAnyArgs(Observable.Return<CalendarItem?>(null));

                ViewModel.OnCalendarEventLongPressed.Inputs.OnNext(calendarEvent);

                InteractorFactory
                    .DidNotReceive()
                    .CreateTimeEntry(Arg.Any<ITimeEntryPrototype>());
            }

            [Fact, LogIfTooSlow]
            public void AllowsCopyingOfTheCalendarEventIntoATimeEntry()
            {
                selectOptionByOptionText(Resources.CalendarCopyEventToTimeEntry);

                ViewModel.OnCalendarEventLongPressed.Inputs.OnNext(calendarEvent);

                InteractorFactory
                    .Received()
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(
                        te => te.StartTime == calendarEvent.StartTime && te.Duration == calendarEvent.Duration));
            }

            [Fact, LogIfTooSlow]
            public void AllowsStartingTheCalendarEventWithTheStartTimeSetToNow()
            {
                selectOptionByOptionText(Resources.CalendarStartNow);

                ViewModel.OnCalendarEventLongPressed.Inputs.OnNext(calendarEvent);

                InteractorFactory
                    .Received()
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(
                        te => te.StartTime == Now && te.Duration == null));
            }

            [Fact, LogIfTooSlow]
            public void AllowsStartingTheCalendarEventWithTheStartTimeSetToTheCalendarEventStartWhenItStartsInThePast()
            {
                TimeService.CurrentDateTime.Returns(calendarEvent.StartTime + TimeSpan.FromMinutes(37));
                selectOptionByOptionText(Resources.CalendarStartWhenTheEventStarts);

                ViewModel.OnCalendarEventLongPressed.Inputs.OnNext(calendarEvent);

                InteractorFactory
                    .Received()
                    .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(
                        te => te.StartTime == calendarEvent.StartTime && te.Duration == null));
            }

            private void selectOptionByOptionText(string text)
            {
                DialogService.Select<CalendarItem?>(null, null, 0)
                    .ReturnsForAnyArgs(callInfo =>
                    {
                        var copyOption = callInfo.Arg<IEnumerable<(string, CalendarItem?)>>()
                            .Single(option => option.Item1 == text)
                            .Item2;
                        return Observable.Return(copyOption);
                    });
            }
        }
    }
}
