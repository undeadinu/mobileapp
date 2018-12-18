using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using MvvmCross.Binding.Extensions;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Experiments;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Hints;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Xunit;
using static Toggl.Foundation.Helper.Constants;
using Notification = System.Reactive.Notification;
using ThreadingTask = System.Threading.Tasks.Task;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class MainViewModelTests
    {
        public abstract class MainViewModelTest : BaseViewModelTests<MainViewModel>
        {
            protected ISubject<SyncProgress> ProgressSubject { get; } = new Subject<SyncProgress>();

            protected override MainViewModel CreateViewModel()
            {
                var vm = new MainViewModel(
                    DataSource,
                    TimeService,
                    RatingService,
                    UserPreferences,
                    AnalyticsService,
                    OnboardingStorage,
                    InteractorFactory,
                    NavigationService,
                    RemoteConfigService,
                    SuggestionProviderContainer,
                    IntentDonationService,
                    AccessRestrictionStorage,
                    SchedulerProvider,
                    StopwatchProvider,
                    RxActionFactory);

                vm.Prepare();

                return vm;
            }

            protected override void AdditionalSetup()
            {
                base.AdditionalSetup();

                var syncManager = Substitute.For<ISyncManager>();
                syncManager.ProgressObservable.Returns(ProgressSubject.AsObservable());
                DataSource.SyncManager.Returns(syncManager);

                var defaultRemoteConfiguration = new RatingViewConfiguration(5, RatingViewCriterion.None);
                RemoteConfigService
                    .RatingViewConfiguration
                    .Returns(Observable.Return(defaultRemoteConfiguration));

                var provider = Substitute.For<ISuggestionProvider>();
                provider.GetSuggestions().Returns(Observable.Empty<Suggestion>());
                SuggestionProviderContainer.Providers.Returns(new[] { provider }.ToList().AsReadOnly());
            }
        }

        public sealed class TheConstructor : MainViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useTimeService,
                bool useRatingService,
                bool useUserPreferences,
                bool useAnalyticsService,
                bool useOnboardingStorage,
                bool useInteractorFactory,
                bool useNavigationService,
                bool useRemoteConfigService,
                bool useSuggestionProviderContainer,
                bool useIntentDonationService,
                bool useAccessRestrictionStorage,
                bool useSchedulerProvider,
                bool useStopwatchProvider,
                bool useRxActionFactory)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var ratingService = useRatingService ? RatingService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var remoteConfigService = useRemoteConfigService ? RemoteConfigService : null;
                var suggestionProviderContainer = useSuggestionProviderContainer ? SuggestionProviderContainer : null;
                var intentDonationService = useIntentDonationService ? IntentDonationService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var accessRestrictionStorage = useAccessRestrictionStorage ? AccessRestrictionStorage : null;
                var stopwatchProvider = useStopwatchProvider ? StopwatchProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new MainViewModel(
                        dataSource,
                        timeService,
                        ratingService,
                        userPreferences,
                        analyticsService,
                        onboardingStorage,
                        interactorFactory,
                        navigationService,
                        remoteConfigService,
                        suggestionProviderContainer,
                        intentDonationService,
                        accessRestrictionStorage,
                        schedulerProvider,
                        stopwatchProvider,
                        rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheViewAppearingMethod : MainViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask NavigatesToNoWorkspaceViewModelWhenNoWorkspaceStateIsSet()
            {
                AccessRestrictionStorage.HasNoWorkspace().Returns(true);

                ViewModel.ViewAppearing();

                await NavigationService.Received().Navigate<NoWorkspaceViewModel, Unit>();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask DoesNotNavigateToNoWorkspaceViewModelWhenNoWorkspaceStateIsNotSet()
            {
                AccessRestrictionStorage.HasNoWorkspace().Returns(false);

                ViewModel.ViewAppearing();

                await NavigationService.DidNotReceive().Navigate<NoWorkspaceViewModel, Unit>();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask DoesNotNavigateToNoWorkspaceViewSeveralTimes()
            {
                AccessRestrictionStorage.HasNoWorkspace().Returns(true);
                var task = new TaskCompletionSource<Unit>().Task;
                NavigationService.Navigate<NoWorkspaceViewModel, Unit>().Returns(task);

                ViewModel.ViewAppearing();
                ViewModel.ViewAppearing();
                ViewModel.ViewAppearing();
                ViewModel.ViewAppearing();

                await NavigationService.Received(1).Navigate<NoWorkspaceViewModel, Unit>();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask NavigatesToSelectDefaultWorkspaceViewModelWhenNoDefaultWorkspaceStateIsSet()
            {
                AccessRestrictionStorage.HasNoWorkspace().Returns(false);
                AccessRestrictionStorage.HasNoDefaultWorkspace().Returns(true);

                await ViewModel.ViewAppearingAsync();

                await NavigationService.Received().Navigate<SelectDefaultWorkspaceViewModel, Unit>();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask DoesNotNavigateToSelectDefaultWorkspaceViewModelWhenNoDefaultWorkspaceStateIsNotSet()
            {
                AccessRestrictionStorage.HasNoDefaultWorkspace().Returns(false);

                ViewModel.ViewAppearing();

                await NavigationService.DidNotReceive().Navigate<SelectDefaultWorkspaceViewModel, Unit>();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask DoesNotNavigateToSelectDefaultWorkspaceViewSeveralTimes()
            {
                AccessRestrictionStorage.HasNoWorkspace().Returns(false);
                AccessRestrictionStorage.HasNoDefaultWorkspace().Returns(true);
                var task = new TaskCompletionSource<Unit>().Task;
                NavigationService.Navigate<SelectDefaultWorkspaceViewModel, Unit>().Returns(task);

                ViewModel.ViewAppearing();
                ViewModel.ViewAppearing();
                ViewModel.ViewAppearing();
                ViewModel.ViewAppearing();
                //ViewAppearing calls an async method. The delay is here to ensure that the async method completes before the assertion
                await ThreadingTask.Delay(200);

                await NavigationService.Received(1).Navigate<SelectDefaultWorkspaceViewModel, Unit>();
            }
        }

        public sealed class TheStartTimeEntryAction : MainViewModelTest
        {
            private readonly ISubject<IThreadSafeTimeEntry> subject = new Subject<IThreadSafeTimeEntry>();

            public TheStartTimeEntryAction()
            {
                DataSource.TimeEntries.CurrentlyRunningTimeEntry.Returns(subject);
                TimeService.CurrentDateTime.Returns(DateTimeOffset.Now);
                ViewModel.Initialize().GetAwaiter().GetResult();

                subject.OnNext(null);
                TestScheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);
            }

            [Theory, LogIfTooSlow]
            [InlineData(true, true)]
            [InlineData(true, false)]
            [InlineData(false, true)]
            [InlineData(false, false)]
            public async ThreadingTask NavigatesToTheStartTimeEntryViewModel(bool isInManualMode, bool useDefaultMode)
            {
                UserPreferences.IsManualModeEnabled.Returns(isInManualMode);

                ViewModel.StartTimeEntry.Execute(useDefaultMode);

                TestScheduler.Start();
                await NavigationService.Received()
                   .Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(Arg.Any<StartTimeEntryParameters>());
            }

            [Theory, LogIfTooSlow]
            [InlineData(true, true)]
            [InlineData(true, false)]
            [InlineData(false, true)]
            [InlineData(false, false)]
            public async ThreadingTask PassesTheAppropriatePlaceholderToTheStartTimeEntryViewModel(bool isInManualMode, bool useDefaultMode)
            {
                UserPreferences.IsManualModeEnabled.Returns(isInManualMode);

                ViewModel.StartTimeEntry.Execute(useDefaultMode);

                TestScheduler.Start();
                var expected = isInManualMode == useDefaultMode
                    ? Resources.ManualTimeEntryPlaceholder
                    : Resources.StartTimeEntryPlaceholder;
                await NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                    Arg.Is<StartTimeEntryParameters>(parameter => parameter.PlaceholderText == expected)
                );
            }

            [Theory, LogIfTooSlow]
            [InlineData(true, true)]
            [InlineData(true, false)]
            [InlineData(false, true)]
            [InlineData(false, false)]
            public async ThreadingTask PassesTheAppropriateDurationToTheStartTimeEntryViewModel(bool isInManualMode, bool useDefaultMode)
            {
                UserPreferences.IsManualModeEnabled.Returns(isInManualMode);

                ViewModel.StartTimeEntry.Execute(useDefaultMode);

                TestScheduler.Start();
                var expected = isInManualMode == useDefaultMode
                    ? TimeSpan.FromMinutes(DefaultTimeEntryDurationForManualModeInMinutes)
                    : (TimeSpan?)null;
                await NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                    Arg.Is<StartTimeEntryParameters>(parameter => parameter.Duration == expected)
                );
            }

            [Theory, LogIfTooSlow]
            [InlineData(true, true)]
            [InlineData(true, false)]
            [InlineData(false, true)]
            [InlineData(false, false)]
            public async ThreadingTask PassesTheAppropriateStartTimeToTheStartTimeEntryViewModel(bool isInManualMode, bool useDefaultMode)
            {
                var date = DateTimeOffset.Now;
                TimeService.CurrentDateTime.Returns(date);
                UserPreferences.IsManualModeEnabled.Returns(isInManualMode);

                ViewModel.StartTimeEntry.Execute(useDefaultMode);

                TestScheduler.Start();
                var expected = isInManualMode == useDefaultMode
                    ? date.Subtract(TimeSpan.FromMinutes(DefaultTimeEntryDurationForManualModeInMinutes))
                    : date;
                await NavigationService.Received().Navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(
                    Arg.Is<StartTimeEntryParameters>(parameter => parameter.StartTime == expected)
                );
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public void CannotBeExecutedWhenThereIsARunningTimeEntry(bool useDefaultMode)
            {
                var timeEntry = new MockTimeEntry();
                subject.OnNext(timeEntry);
                TestScheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);

                var observer = TestScheduler.CreateObserver<Unit>();
                ViewModel.StartTimeEntry.Execute(useDefaultMode)
                    .Subscribe(observer);
                TestScheduler.Start();

                observer.Messages.Count.Should().Be(1);
                observer.Messages.Last().Value.Exception.Should().BeEquivalentTo(new RxActionNotEnabledException());
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public void MarksTheActionButtonTappedForOnboardingPurposes(bool useDefaultMode)
            {
                ViewModel.StartTimeEntry.Execute(useDefaultMode);

                TestScheduler.Start();
                OnboardingStorage.Received().StartButtonWasTapped();
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async ThreadingTask MarksTheActionNavigatedAwayBeforeStopButtonForOnboardingPurposes(bool useDefaultMode)
            {
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(false));
                await ViewModel.Initialize();
                subject.OnNext(null);
                TestScheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);

                ViewModel.StartTimeEntry.Execute(useDefaultMode);

                TestScheduler.Start();
                OnboardingStorage.DidNotReceive().SetNavigatedAwayFromMainViewAfterStopButton();
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async ThreadingTask MarksTheActionNavigatedAwayAfterStopButtonForOnboardingPurposes(bool useDefaultMode)
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                var observable = Observable.Return<IThreadSafeTimeEntry>(null);
                DataSource.TimeEntries.CurrentlyRunningTimeEntry.Returns(observable);
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(true));
                await ViewModel.Initialize();
                subject.OnNext(null);
                TestScheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);

                ViewModel.StartTimeEntry.Execute(useDefaultMode);

                TestScheduler.Start();
                OnboardingStorage.Received().SetNavigatedAwayFromMainViewAfterStopButton();
            }
        }

        public sealed class TheOpenSettingsCommand : MainViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask NavigatesToTheSettingsViewModel()
            {
                ViewModel.Initialize().Wait();

                ViewModel.OpenSettings.Execute();

                TestScheduler.Start();
                await NavigationService.Received().Navigate<SettingsViewModel>();
            }

            [Fact, LogIfTooSlow]
            public void MarksTheActionBeforeStopButtonForOnboardingPurposes()
            {
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(false));
                ViewModel.Initialize().Wait();

                ViewModel.OpenSettings.Execute();

                TestScheduler.Start();
                OnboardingStorage.DidNotReceive().SetNavigatedAwayFromMainViewAfterStopButton();
            }

            [Fact, LogIfTooSlow]
            public void MarksTheActionAfterStopButtonForOnboardingPurposes()
            {
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(true));
                ViewModel.Initialize().Wait();

                ViewModel.OpenSettings.Execute();

                TestScheduler.Start();
                OnboardingStorage.Received().SetNavigatedAwayFromMainViewAfterStopButton();
            }
        }

        public sealed class TheOpenReportsCommand : MainViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask NavigatesToTheReportsViewModel()
            {
                const long workspaceId = 10;
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                workspace.Id.Returns(workspaceId);
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(workspace));
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(false));
                ViewModel.Initialize().Wait();

                ViewModel.OpenReports.Execute();

                TestScheduler.Start();
                await NavigationService.Received().Navigate<ReportsViewModel>();
            }

            [Fact, LogIfTooSlow]
            public void MarksTheActionBeforeStopButtonForOnboardingPurposes()
            {
                const long workspaceId = 10;
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                workspace.Id.Returns(workspaceId);
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(workspace));
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(false));
                ViewModel.Initialize().Wait();

                ViewModel.OpenReports.Execute();

                TestScheduler.Start();
                OnboardingStorage.DidNotReceive().SetNavigatedAwayFromMainViewAfterStopButton();
            }

            [Fact, LogIfTooSlow]
            public void MarksTheActionAfterStopButtonForOnboardingPurposes()
            {
                const long workspaceId = 10;
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                workspace.Id.Returns(workspaceId);
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(workspace));
                OnboardingStorage.StopButtonWasTappedBefore.Returns(Observable.Return(true));
                ViewModel.Initialize().Wait();

                ViewModel.OpenReports.Execute();

                TestScheduler.Start();
                OnboardingStorage.Received().SetNavigatedAwayFromMainViewAfterStopButton();
            }
        }

        public sealed class TheOpenSyncFailuresCommand : MainViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask NavigatesToTheSyncFailuresViewModel()
            {
                ViewModel.Initialize().Wait();

                ViewModel.OpenSyncFailures.Execute();

                TestScheduler.Start();
                await NavigationService.Received().Navigate<SyncFailuresViewModel>();
            }
        }

        public class TheStopTimeEntryAction : MainViewModelTest
        {
            private ISubject<IThreadSafeTimeEntry> subject;

            public TheStopTimeEntryAction()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                subject = new BehaviorSubject<IThreadSafeTimeEntry>(timeEntry);
                DataSource.TimeEntries.CurrentlyRunningTimeEntry.Returns(subject);

                ViewModel.Initialize().Wait();
                TestScheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask CallsTheStopMethodOnTheDataSource()
            {
                var date = DateTimeOffset.UtcNow;
                TimeService.CurrentDateTime.Returns(date);

                ViewModel.StopTimeEntry.Execute(TimeEntryStopOrigin.Deeplink);

                TestScheduler.Start();
                await InteractorFactory.Received().StopTimeEntry(date, TimeEntryStopOrigin.Deeplink).Execute();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask InitiatesPushSync()
            {
                ViewModel.StopTimeEntry.Execute(Arg.Any<TimeEntryStopOrigin>());

                TestScheduler.Start();
                await DataSource.SyncManager.Received().PushSync();
            }

            [Fact, LogIfTooSlow]
            public void MarksTheActionForOnboardingPurposes()
            {
                ViewModel.StopTimeEntry.Execute(Arg.Any<TimeEntryStopOrigin>());

                TestScheduler.Start();
                OnboardingStorage.Received().StopButtonWasTapped();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask DoesNotInitiatePushSyncWhenSavingFails()
            {
                InteractorFactory
                    .StopTimeEntry(Arg.Any<DateTimeOffset>(), Arg.Any<TimeEntryStopOrigin>())
                    .Execute()
                    .Returns(Observable.Throw<IThreadSafeTimeEntry>(new Exception()));

                var observer = TestScheduler.CreateObserver<Unit>();
                ViewModel.StopTimeEntry.Execute(Arg.Any<TimeEntryStopOrigin>())
                    .Subscribe(observer);
                TestScheduler.Start();

                observer.Messages.Count().Should().Be(1);
                observer.Messages.Last().Value.Kind.Should().Be(NotificationKind.OnError);
                await DataSource.SyncManager.DidNotReceive().PushSync();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask CannotBeExecutedWhenNoTimeEntryIsRunning()
            {
                subject.OnNext(null);
                TestScheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);

                var observer = TestScheduler.CreateObserver<Unit>();
                ViewModel.StopTimeEntry.Execute(TimeEntryStopOrigin.Manual)
                    .Subscribe(observer);
                TestScheduler.Start();

                observer.Messages.Count.Should().Be(1);
                observer.Messages.Last().Value.Exception.Should().BeEquivalentTo(new RxActionNotEnabledException());

                await InteractorFactory.DidNotReceive().StopTimeEntry(Arg.Any<DateTimeOffset>(), Arg.Any<TimeEntryStopOrigin>()).Execute();
            }

            [Fact, LogIfTooSlow]
            public void ShouldDonateStopTimerIntent()
            {
                var secondTimeEntry = Substitute.For<IThreadSafeTimeEntry>();

                ViewModel.StopTimeEntry.Execute(Arg.Any<TimeEntryStopOrigin>());
                TestScheduler.Start();
                TestScheduler.Stop();
                subject.OnNext(secondTimeEntry);
                TestScheduler.AdvanceBy(TimeSpan.FromMilliseconds(50).Ticks);
                ViewModel.StopTimeEntry.Execute(Arg.Any<TimeEntryStopOrigin>());

                TestScheduler.Start();
                IntentDonationService.Received().DonateStopCurrentTimeEntry();
            }
        }

        public sealed class TheNumberOfSyncFailuresProperty : MainViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsTheCountOfInteractorResult()
            {
                var syncables = new IDatabaseSyncable[]
                {
                    new MockTag { Name = "Tag", SyncStatus = SyncStatus.SyncFailed, LastSyncErrorMessage = "Error1" },
                    new MockTag { Name = "Tag2", SyncStatus = SyncStatus.SyncFailed, LastSyncErrorMessage = "Error1" },
                    new MockProject { Name = "Project", SyncStatus = SyncStatus.SyncFailed, LastSyncErrorMessage = "Error2" }
                };
                var items = syncables.Select(i => new SyncFailureItem(i));
                var interactor = Substitute.For<IInteractor<IObservable<IEnumerable<SyncFailureItem>>>>();
                interactor.Execute().Returns(Observable.Return(items));
                InteractorFactory.GetItemsThatFailedToSync().Returns(interactor);
                await ViewModel.Initialize();

                var observer = TestScheduler.CreateObserver<int>();
                ViewModel.NumberOfSyncFailures.Subscribe(observer);
                TestScheduler.AdvanceBy(50);

                observer.Messages
                    .Last(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).Value.Value
                    .Should()
                    .Be(syncables.Length);
            }
        }

        public abstract class InitialStateTest : MainViewModelTest
        {
            protected void PrepareSuggestion()
            {
                DataSource.TimeEntries.IsEmpty.Returns(Observable.Return(false));
                var suggestionProvider = Substitute.For<ISuggestionProvider>();
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(123);
                timeEntry.Start.Returns(DateTimeOffset.Now);
                timeEntry.Duration.Returns((long?)null);
                timeEntry.Description.Returns("something");
                var suggestion = new Suggestion(timeEntry);
                suggestionProvider.GetSuggestions().Returns(Observable.Return(suggestion));
                var providers = new ReadOnlyCollection<ISuggestionProvider>(
                    new List<ISuggestionProvider> { suggestionProvider }
                );
                SuggestionProviderContainer.Providers.Returns(providers);
            }

            protected void PrepareTimeEntry()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(123);
                timeEntry.Start.Returns(DateTimeOffset.Now);
                timeEntry.Duration.Returns(100);
                InteractorFactory.GetAllTimeEntriesVisibleToTheUser().Execute()
                    .Returns(Observable.Return(new[] { timeEntry }));
                DataSource
                    .TimeEntries
                    .Updated
                    .Returns(Observable.Never<EntityUpdate<IThreadSafeTimeEntry>>());
            }

            protected void PrepareIsWelcome(bool isWelcome)
            {
                var subject = new BehaviorSubject<bool>(isWelcome);
                OnboardingStorage.IsNewUser.Returns(subject.AsObservable());
            }
        }

        public sealed class TheShouldShowEmptyStateProperty : InitialStateTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsTrueWhenThereAreNoSuggestionsAndNoTimeEntriesAndIsWelcome()
            {
                PrepareIsWelcome(true);
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<bool>();

                ViewModel.ShouldShowEmptyState.Subscribe(observer);

                TestScheduler.Start();
                observer.Messages.Last().Value.Value.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsFalseWhenThereAreSomeSuggestions()
            {
                PrepareSuggestion();
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<bool>();

                ViewModel.ShouldShowEmptyState.Subscribe(observer);

                TestScheduler.Start();
                observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsFalseWhenThereAreSomeTimeEntries()
            {
                PrepareTimeEntry();
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<bool>();

                ViewModel.ShouldShowEmptyState.Subscribe(observer);

                TestScheduler.Start();
                observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsFalseWhenIsNotWelcome()
            {
                PrepareIsWelcome(false);
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<bool>();

                ViewModel.ShouldShowEmptyState.Subscribe(observer);

                TestScheduler.Start();

                observer.Messages.Last().Value.Value.Should().BeFalse();
            }
        }

        public sealed class TheShouldShowWelcomeBackProperty : InitialStateTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsTrueWhenThereAreNoSuggestionsAndNoTimeEntriesAndIsNotWelcome()
            {
                PrepareIsWelcome(false);
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<bool>();

                ViewModel.ShouldShowWelcomeBack.Subscribe(observer);

                TestScheduler.Start();
                observer.Messages.AssertEqual(
                    ReactiveTest.OnNext(1, false),
                    ReactiveTest.OnNext(2, true)
                );
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsFalseWhenThereAreSomeSuggestions()
            {
                PrepareSuggestion();
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<bool>();

                ViewModel.ShouldShowWelcomeBack.Subscribe(observer);

                TestScheduler.Start();
                observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsFalseWhenThereAreSomeTimeEntries()
            {
                PrepareTimeEntry();
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<bool>();

                ViewModel.ShouldShowWelcomeBack.Subscribe(observer);

                TestScheduler.Start();
                observer.Messages.Last().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async ThreadingTask ReturnsFalseWhenIsWelcome()
            {
                PrepareIsWelcome(true);
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<bool>();

                ViewModel.ShouldShowWelcomeBack.Subscribe(observer);

                TestScheduler.Start();
                observer.Messages.Last().Value.Value.Should().BeFalse();
            }
        }

        public sealed class TheInitializeMethod
        {
            public sealed class WhenReceivingADescription : MainViewModelTest
            {
                [Fact, LogIfTooSlow]
                public async ThreadingTask CreatesANewTimeEntryWithThePassedDescriptionInTheDefaultWorkspace()
                {
                    const string description = "working on something";
                    var defaultWorkspace = new MockWorkspace { Id = 1 };
                    InteractorFactory
                        .GetDefaultWorkspace()
                        .Execute()
                        .Returns(Observable.Return(defaultWorkspace));
                    ViewModel.Init(null, description);

                    await InteractorFactory
                        .Received()
                        .CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(
                                te => te.Description == description
                                   && te.WorkspaceId == defaultWorkspace.Id))
                        .Execute();
                }
            }

            public sealed class WhenNavigationActionIsStop : MainViewModelTest
            {
                [Fact, LogIfTooSlow]
                public async ThreadingTask StopsTheCurrentEntry()
                {
                    ViewModel.Init(ApplicationUrls.Main.Action.Stop, null);
                    await ViewModel.Initialize();

                    await InteractorFactory.Received().StopTimeEntry(TimeService.CurrentDateTime, Arg.Any<TimeEntryStopOrigin>()).Execute();
                }

                [Fact, LogIfTooSlow]
                public async ThreadingTask StartsPushSync()
                {
                    ViewModel.Init(ApplicationUrls.Main.Action.Stop, null);
                    await ViewModel.Initialize();

                    await DataSource.SyncManager.Received().PushSync();
                }
            }

            public sealed class WhenNavigationActionIsContinue : MainViewModelTest
            {
                [Fact, LogIfTooSlow]
                public async ThreadingTask GetsTheContinueMostRecentTimeEntryInteractor()
                {
                    ViewModel.Init(ApplicationUrls.Main.Action.Continue, null);

                    await ViewModel.Initialize();

                    InteractorFactory.Received().ContinueMostRecentTimeEntry();
                }

                [Fact, LogIfTooSlow]
                public async ThreadingTask ExecutesTheContinueMostRecentTimeEntryInteractor()
                {
                    var interactor = Substitute.For<IInteractor<IObservable<IThreadSafeTimeEntry>>>();
                    InteractorFactory.ContinueMostRecentTimeEntry().Returns(interactor);
                    ViewModel.Init(ApplicationUrls.Main.Action.Continue, null);

                    await ViewModel.Initialize();

                    await interactor.Received().Execute();
                }
            }

            public sealed class WhenShowingTheRatingsView : MainViewModelTest
            {
                [Fact, LogIfTooSlow]
                public async void DoesNotShowTheRatingViewByDefault()
                {
                    await ViewModel.Initialize();
                    await NavigationService.DidNotReceive().ChangePresentation(
                        Arg.Is<ToggleRatingViewVisibilityHint>(hint => hint.ShouldHide == false)
                    );
                }

                [Fact, LogIfTooSlow]
                public async void ShowsTheRatingView()
                {
                    var defaultRemoteConfiguration = new RatingViewConfiguration(5, RatingViewCriterion.Start);
                    RemoteConfigService
                        .RatingViewConfiguration
                        .Returns(Observable.Return(defaultRemoteConfiguration));

                    var now = DateTimeOffset.Now;
                    var firstOpened = now - TimeSpan.FromDays(5);

                    TimeService.CurrentDateTime.Returns(now);
                    OnboardingStorage.GetFirstOpened().Returns(firstOpened);

                    await ViewModel.Initialize();
                    await NavigationService.Received().ChangePresentation(
                        Arg.Is<ToggleRatingViewVisibilityHint>(hint => hint.ShouldHide == false)
                    );
                }

                [Fact, LogIfTooSlow]
                public async void DoesNotShowTheRatingViewIfThereWasAnInteraction()
                {
                    var defaultRemoteConfiguration = new RatingViewConfiguration(5, RatingViewCriterion.Start);
                    RemoteConfigService
                        .RatingViewConfiguration
                        .Returns(Observable.Return(defaultRemoteConfiguration));

                    var now = DateTimeOffset.Now;
                    var firstOpened = now - TimeSpan.FromDays(6);

                    TimeService.CurrentDateTime.Returns(now);
                    OnboardingStorage.GetFirstOpened().Returns(firstOpened);
                    OnboardingStorage.RatingViewOutcome().Returns(RatingViewOutcome.AppWasNotRated);

                    await ViewModel.Initialize();
                    await NavigationService.DidNotReceive().ChangePresentation(
                        Arg.Is<ToggleRatingViewVisibilityHint>(hint => hint.ShouldHide == false)
                    );
                }

                [Fact, LogIfTooSlow]
                public async void DoesNotShowTheRatingViewIfAfter24HourSnooze()
                {
                    var defaultRemoteConfiguration = new RatingViewConfiguration(5, RatingViewCriterion.Start);
                    RemoteConfigService
                        .RatingViewConfiguration
                        .Returns(Observable.Return(defaultRemoteConfiguration));

                    var now = DateTimeOffset.Now;
                    var firstOpened = now - TimeSpan.FromDays(6);
                    var lastInteraction = now - TimeSpan.FromDays(2);

                    TimeService.CurrentDateTime.Returns(now);
                    OnboardingStorage.GetFirstOpened().Returns(firstOpened);
                    OnboardingStorage.RatingViewOutcome().Returns(RatingViewOutcome.AppWasNotRated);
                    OnboardingStorage.RatingViewOutcomeTime().Returns(lastInteraction);

                    await ViewModel.Initialize();
                    await NavigationService.DidNotReceive().ChangePresentation(
                        Arg.Is<ToggleRatingViewVisibilityHint>(hint => hint.ShouldHide == false)
                    );
                }
            }

            public sealed class InvokeIntentDonationService : MainViewModelTest
            {
                [Fact, LogIfTooSlow]
                public async void ShouldSetShortcutSuggestions()
                {
                    await ViewModel.Initialize();
                    IntentDonationService.Received().SetDefaultShortcutSuggestions(Arg.Any<IWorkspace>());
                }
            }
        }
    }
}
