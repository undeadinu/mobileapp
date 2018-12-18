using System;
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
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Settings;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SettingsViewModelTests
    {
        public abstract class SettingsViewModelTest : BaseViewModelTests<SettingsViewModel>
        {
            protected ISubject<IThreadSafeUser> UserSubject;
            protected ISubject<SyncProgress> ProgressSubject;
            protected ISubject<IThreadSafePreferences> PreferencesSubject;

            protected override SettingsViewModel CreateViewModel()
            {
                UserSubject = new Subject<IThreadSafeUser>();
                ProgressSubject = new Subject<SyncProgress>();
                PreferencesSubject = new Subject<IThreadSafePreferences>();

                DataSource.User.Current.Returns(UserSubject.AsObservable());
                DataSource.Preferences.Current.Returns(PreferencesSubject.AsObservable());
                DataSource.SyncManager.ProgressObservable.Returns(ProgressSubject.AsObservable());

                UserSubject.OnNext(new MockUser());
                PreferencesSubject.OnNext(new MockPreferences());

                SetupObservables();

                return new SettingsViewModel(
                    UserAgent,
                    DataSource,
                    UserAccessManager,
                    DialogService,
                    UserPreferences,
                    FeedbackService,
                    AnalyticsService,
                    InteractorFactory,
                    PlatformConstants,
                    OnboardingStorage,
                    NavigationService,
                    PrivateSharedStorageService,
                    IntentDonationService,
                    StopwatchProvider,
                    RxActionFactory);
            }

            protected virtual void SetupObservables()
            {
            }
        }

        public sealed class TheConstructor : SettingsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useUserAgent,
                bool useDataSource,
                bool useUserAccessManager,
                bool useDialogService,
                bool useUserPreferences,
                bool useFeedbackService,
                bool useAnalyticsService,
                bool useInteractorFactory,
                bool usePlatformConstants,
                bool useOnboardingStorage,
                bool useNavigationService,
                bool usePrivateSharedStorageService,
                bool useIntentDonationService,
                bool useStopwatchProvider,
                bool useRxActionFactory)
            {
                var userAgent = useUserAgent ? UserAgent : null;
                var dataSource = useDataSource ? DataSource : null;
                var userAccessManager = useUserAccessManager ? UserAccessManager : null;
                var dialogService = useDialogService ? DialogService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var feedbackService = useFeedbackService ? FeedbackService : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var platformConstants = usePlatformConstants ? PlatformConstants : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var privateSharedStorageService = usePrivateSharedStorageService ? PrivateSharedStorageService : null;
                var intentDonationService = useIntentDonationService ? IntentDonationService : null;
                var stopwatchProvider = useStopwatchProvider ? StopwatchProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SettingsViewModel(
                        userAgent,
                        dataSource,
                        userAccessManager,
                        dialogService,
                        userPreferences,
                        feedbackService,
                        analyticsService,
                        interactorFactory,
                        platformConstants,
                        onboardingStorage,
                        navigationService,
                        privateSharedStorageService,
                        intentDonationService,
                        stopwatchProvider,
                        rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheFlags : SettingsViewModelTest
        {
            [Property]
            public void DoesNotEverSetBothIsRunningSyncAndIsSyncedBothToTrue(NonEmptyArray<SyncProgress> statuses)
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(false));
                var syncedObserver = TestScheduler.CreateObserver<bool>();
                var syncingObserver = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();
                viewModel.IsSynced.Subscribe(syncedObserver);
                viewModel.IsRunningSync.Subscribe(syncingObserver);

                foreach (var state in statuses.Get)
                {
                    syncedObserver.Messages.Clear();
                    syncingObserver.Messages.Clear();

                    ProgressSubject.OnNext(state);

                    var isSynced = syncedObserver.Messages.Single().Value.Value;
                    var isRunningSync = syncingObserver.Messages.Single().Value.Value;

                    (isRunningSync && isSynced).Should().BeFalse();
                }
            }

            [Property]
            public void EmitTheAppropriateIsRunningSyncValues(NonEmptyArray<SyncProgress> statuses)
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(false));
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsRunningSync.Subscribe(observer);

                foreach (var state in statuses.Get)
                {
                    observer.Messages.Clear();

                    ProgressSubject.OnNext(state);

                    var isRunningSync = observer.Messages.Single().Value.Value;
                    isRunningSync.Should().Be(state == SyncProgress.Syncing);
                }
            }

            [Property]
            public void EmitTheAppropriateIsSyncedValues(NonEmptyArray<SyncProgress> statuses)
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsSynced.Subscribe(observer);

                foreach (var state in statuses.Get)
                {
                    if (state == SyncProgress.Unknown)
                        continue;

                    observer.Messages.Clear();

                    ProgressSubject.OnNext(state);

                    var isSynced = observer.Messages.Single().Value.Value;
                    isSynced.Should().Be(state == SyncProgress.Synced);
                }
            }
        }

        public sealed class TheEmailObservable : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void EmitsWheneverTheUserEmailChanges()
            {
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.Email.Subscribe(observer);

                UserSubject.OnNext(new MockUser { Email = Email.From("newmail@mail.com") });
                UserSubject.OnNext(new MockUser { Email = Email.From("newmail@mail.com") });
                UserSubject.OnNext(new MockUser { Email = Email.From("differentmail@mail.com") });

                observer.Messages.Count.Should().Be(2);
            }
        }

        public sealed class TheUserAvatarObservable : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void EmitsWheneverTheUserImageChanges()
            {
                var observer = TestScheduler.CreateObserver<byte[]>();
                ViewModel.UserAvatar.Subscribe(observer);

                UserSubject.OnNext(new MockUser { ImageUrl = "http://toggl.com/image.jpg" });
                UserSubject.OnNext(new MockUser { ImageUrl = "http://toggl.com/image.jpg" });
                UserSubject.OnNext(new MockUser { ImageUrl = "http://toggl.com/image2.jpg" });

                observer.Messages.Count.Should().Be(2);
            }

            [Fact, LogIfTooSlow]
            public void CallsTheImageDownloadInteractor()
            {
                var observer = TestScheduler.CreateObserver<byte[]>();
                ViewModel.UserAvatar.Subscribe(observer);

                UserSubject.OnNext(new MockUser { ImageUrl = "http://toggl.com/image.jpg" });
                UserSubject.OnNext(new MockUser { ImageUrl = "http://toggl.com/image.jpg" });
                UserSubject.OnNext(new MockUser { ImageUrl = "http://toggl.com/image2.jpg" });

                InteractorFactory.Received(2).GetUserAvatar(Arg.Any<string>());
            }
        }

        public sealed class TheTryLogoutMethod : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task EmitsOneIsLoggingOutEvent()
            {
                var observer = TestScheduler.CreateObserver<Unit>();
                ViewModel.LoggingOut.Subscribe(observer);

                doNotShowConfirmationDialog();

                ViewModel.TryLogout.Execute();
                TestScheduler.Start();

                observer.Messages.Single();
            }

            [Fact, LogIfTooSlow]
            public async Task CallsLogoutOnTheDataSource()
            {
                doNotShowConfirmationDialog();

                ViewModel.TryLogout.Execute();
                TestScheduler.Start();

                await UserAccessManager.Received().Logout();
            }

            [Fact, LogIfTooSlow]
            public async Task ResetsUserPreferences()
            {
                doNotShowConfirmationDialog();

                ViewModel.TryLogout.Execute();
                TestScheduler.Start();

                UserPreferences.Received().Reset();
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheLoginScreen()
            {
                doNotShowConfirmationDialog();

                ViewModel.TryLogout.Execute();
                TestScheduler.Start();

                await NavigationService.Received().Navigate<LoginViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task ChecksIfThereAreUnsyncedDataWhenTheSyncProcessFinishes()
            {
                ProgressSubject.OnNext(SyncProgress.Synced);

                ViewModel.TryLogout.Execute();
                TestScheduler.Start();

                await DataSource.Received().HasUnsyncedData();
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotShowConfirmationDialogWhenTheAppIsInSync()
            {
                doNotShowConfirmationDialog();

                ViewModel.TryLogout.Execute();
                TestScheduler.Start();

                await DialogService.DidNotReceiveWithAnyArgs().Confirm("", "", "", "");
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsConfirmationDialogWhenThereIsNothingToPushButSyncIsRunning()
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(false));
                ProgressSubject.OnNext(SyncProgress.Syncing);

                ViewModel.TryLogout.Execute();
                TestScheduler.Start();

                await DialogService.ReceivedWithAnyArgs().Confirm("", "", "", "");
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsConfirmationDialogWhenThereIsSomethingToPushButSyncIsNotRunning()
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(true));
                ProgressSubject.OnNext(SyncProgress.Syncing);

                ViewModel.TryLogout.Execute();
                TestScheduler.Start();

                await DialogService.ReceivedWithAnyArgs().Confirm("", "", "", "");
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotProceedWithLogoutWhenUserClicksCancelButtonInTheDialog()
            {
                ProgressSubject.OnNext(SyncProgress.Syncing);
                DialogService.Confirm(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>()).Returns(Observable.Return(false));

                ViewModel.TryLogout.Execute();
                TestScheduler.Start();

                await UserAccessManager.DidNotReceive().Logout();
                await NavigationService.DidNotReceive().Navigate<LoginViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task ProceedsWithLogoutWhenUserClicksSignOutButtonInTheDialog()
            {
                ProgressSubject.OnNext(SyncProgress.Syncing);
                DialogService.Confirm(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>()).Returns(Observable.Return(true));

                ViewModel.TryLogout.Execute();
                TestScheduler.Start();

                await UserAccessManager.Received().Logout();
                await NavigationService.Received().Navigate<LoginViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksLogoutEvent()
            {
                doNotShowConfirmationDialog();

                ViewModel.TryLogout.Execute();
                TestScheduler.Start();

                AnalyticsService.Logout.Received().Track(Analytics.LogoutSource.Settings);
            }

            private void doNotShowConfirmationDialog()
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(false));
                ProgressSubject.OnNext(SyncProgress.Synced);
            }

            [Fact, LogIfTooSlow]
            public async Task ClearsPrivateSharedStorage()
            {
                doNotShowConfirmationDialog();

                ViewModel.TryLogout.Execute();
                TestScheduler.Start();

                PrivateSharedStorageService.Received().ClearAll();
            }

            [Fact, LogIfTooSlow]
            public async Task ClearsDonatedIntents()
            {
                doNotShowConfirmationDialog();

                ViewModel.TryLogout.Execute();
                TestScheduler.Start();

                IntentDonationService.Received().ClearAll();
            }
        }

        public sealed class ThePickDefaultWorkspaceMethod : SettingsViewModelTest
        {
            private const long workspaceId = 10;
            private const long defaultWorkspaceId = 11;
            private const string workspaceName = "My custom workspace";
            private readonly IThreadSafeWorkspace workspace;
            private readonly IThreadSafeWorkspace defaultWorkspace = Substitute.For<IThreadSafeWorkspace>();

            protected override void SetupObservables()
            {
                DataSource.User.Current.Returns(Observable.Return(new MockUser()));
            }

            public ThePickDefaultWorkspaceMethod()
            {
                defaultWorkspace = new MockWorkspace { Id = defaultWorkspaceId };
                workspace = new MockWorkspace { Id = workspaceId, Name = workspaceName };

                UserSubject.OnNext(new MockUser());

                InteractorFactory.GetDefaultWorkspace().Execute()
                    .Returns(Observable.Return(defaultWorkspace));

                InteractorFactory.GetWorkspaceById(workspaceId).Execute()
                    .Returns(Observable.Return(workspace));

                ViewModel.Prepare();
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheSelectWorkspaceViewModel()
            {
                ViewModel.PickDefaultWorkspace.Execute();
                TestScheduler.Start();

                await NavigationService.Received()
                    .Navigate<SelectWorkspaceViewModel, WorkspaceParameters, long>(Arg.Any<WorkspaceParameters>());
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheUserWithTheReceivedWorspace()
            {
                NavigationService
                    .Navigate<SelectWorkspaceViewModel, WorkspaceParameters, long>(Arg.Any<WorkspaceParameters>())
                    .Returns(Task.FromResult(workspaceId));

                ViewModel.PickDefaultWorkspace.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .UpdateDefaultWorkspace(Arg.Is(workspaceId))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task StartsTheSyncAlgorithm()
            {
                NavigationService
                    .Navigate<SelectWorkspaceViewModel, WorkspaceParameters, long>(Arg.Any<WorkspaceParameters>())
                    .Returns(Task.FromResult(workspaceId));

                ViewModel.PickDefaultWorkspace.Execute();
                TestScheduler.Start();

                await DataSource.SyncManager.Received().PushSync();
            }
        }

        public sealed class TheToggleManualModeMethod : SettingsViewModelTest
        {
            public TheToggleManualModeMethod()
            {
                PreferencesSubject.OnNext(new MockPreferences());
            }

            [Fact, LogIfTooSlow]
            public void CallsEnableTimerModeIfCurrentlyInManualMode()
            {
                UserPreferences.IsManualModeEnabled.Returns(true);

                ViewModel.ToggleManualMode();

                UserPreferences.Received().EnableTimerMode();
            }

            [Fact, LogIfTooSlow]
            public void CallsEnableManualModeIfCurrentlyInTimerMode()
            {
                UserPreferences.IsManualModeEnabled.Returns(false);

                ViewModel.ToggleManualMode();

                UserPreferences.Received().EnableManualMode();
            }
        }

        public sealed class TheOpenHelpViewMethod : SettingsViewModelTest
        {
            [Property]
            public void NavigatesToBrowserViewModelWithUrlFromPlatformConstants(
                NonEmptyString nonEmptyString)
            {
                var helpUrl = nonEmptyString.Get;
                PlatformConstants.HelpUrl.Returns(helpUrl);

                ViewModel.OpenHelpView.Execute();
                TestScheduler.Start();

                NavigationService.Received().Navigate<BrowserViewModel, BrowserParameters>(
                    Arg.Is<BrowserParameters>(parameter => parameter.Url == helpUrl));
            }

            [Fact, LogIfTooSlow]
            public void NavigatesToBrowserViewModelWithHelpTitle()
            {
                ViewModel.OpenHelpView.Execute();

                NavigationService.Received().Navigate<BrowserViewModel, BrowserParameters>(
                    Arg.Is<BrowserParameters>(parameter => parameter.Title == Resources.Help));
            }
        }

        public sealed class TheVersionProperty : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ShouldBeConstructedFromVersionAndBuildNumber()
            {
                ViewModel.Version.Should().Be($"{UserAgent.Version} ({PlatformConstants.BuildNumber})");
            }
        }

        public sealed class TheSelectDateFormatMethod : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToSelectDateFormatViewModelPassingCurrentDateFormat()
            {
                var dateFormat = DateFormat.FromLocalizedDateFormat("MM-DD-YYYY");
                var preferences = new MockPreferences { DateFormat = dateFormat };
                PreferencesSubject.OnNext(preferences);

                ViewModel.SelectDateFormat.Execute();
                TestScheduler.Start();

                await NavigationService
                    .Received()
                    .Navigate<SelectDateFormatViewModel, DateFormat, DateFormat>(dateFormat);
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheStoredPreferences()
            {
                var oldDateFormat = DateFormat.FromLocalizedDateFormat("MM-DD-YYYY");
                var newDateFormat = DateFormat.FromLocalizedDateFormat("DD.MM.YYYY");
                var preferences = new MockPreferences { DateFormat = oldDateFormat };
                PreferencesSubject.OnNext(preferences);
                NavigationService
                    .Navigate<SelectDateFormatViewModel, DateFormat, DateFormat>(Arg.Any<DateFormat>())
                    .Returns(Task.FromResult(newDateFormat));

                ViewModel.SelectDateFormat.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .UpdatePreferences(Arg.Is<EditPreferencesDTO>(dto => dto.DateFormat.Equals(New<DateFormat>.Value(newDateFormat))))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheDateFormatProperty()
            {
                var oldDateFormat = DateFormat.FromLocalizedDateFormat("MM-DD-YYYY");
                var newDateFormat = DateFormat.FromLocalizedDateFormat("DD.MM.YYYY");
                var oldPreferences = new MockPreferences { DateFormat = oldDateFormat };
                var newPreferences = new MockPreferences { DateFormat = newDateFormat };
                PreferencesSubject.OnNext(oldPreferences);
                NavigationService
                    .Navigate<SelectDateFormatViewModel, DateFormat, DateFormat>(Arg.Any<DateFormat>())
                    .Returns(Task.FromResult(newDateFormat));
                InteractorFactory.UpdatePreferences(Arg.Any<EditPreferencesDTO>())
                    .Execute()
                    .Returns(Observable.Return(newPreferences));

                ViewModel.SelectDateFormat.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .UpdatePreferences(Arg.Is<EditPreferencesDTO>(dto => dto.DateFormat.ValueOr(oldDateFormat) == newDateFormat))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task InitiatesPushSync()
            {
                var oldDateFormat = DateFormat.FromLocalizedDateFormat("MM-DD-YYYY");
                var newDateFormat = DateFormat.FromLocalizedDateFormat("DD.MM.YYYY");
                var preferences = new MockPreferences { DateFormat = oldDateFormat };
                PreferencesSubject.OnNext(preferences);
                NavigationService
                    .Navigate<SelectDateFormatViewModel, DateFormat, DateFormat>(Arg.Any<DateFormat>())
                    .Returns(Task.FromResult(newDateFormat));

                ViewModel.SelectDateFormat.Execute();
                TestScheduler.Start();

                await DataSource.SyncManager.Received().PushSync();
            }
        }

        public sealed class TheToggleUseTwentyFourHourClockMethod : SettingsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ChangesTheValueOfTheUseTwentyFourHourHourClock(bool originalValue)
            {
                var timeFormat = originalValue ? TimeFormat.TwentyFourHoursFormat : TimeFormat.TwelveHoursFormat;
                PreferencesSubject.OnNext(new MockPreferences { TimeOfDayFormat = timeFormat });

                ViewModel.ToggleTwentyFourHourSettings.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .UpdatePreferences(Arg.Is<EditPreferencesDTO>(
                        dto => dto.TimeOfDayFormat.ValueOr(default(TimeFormat)).IsTwentyFourHoursFormat != originalValue)
                    ).Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task InitiatesPushSync()
            {
                var preferences = new MockPreferences();
                PreferencesSubject.OnNext(preferences);
                var observable = Observable.Return(preferences);
                InteractorFactory.UpdatePreferences(Arg.Any<EditPreferencesDTO>()).Execute().Returns(observable);

                ViewModel.ToggleTwentyFourHourSettings.Execute();
                TestScheduler.Start();

                await DataSource.SyncManager.Received().PushSync();
            }
        }

        public sealed class TheSelectDurationFormatMethod : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToSelectDurationFormatViewModelPassingCurrentDurationFormat()
            {
                var durationFormat = DurationFormat.Improved;
                var preferences = new MockPreferences { DurationFormat = durationFormat };
                PreferencesSubject.OnNext(preferences);

                ViewModel.SelectDurationFormat.Execute();
                TestScheduler.Start();

                await NavigationService
                    .Received()
                    .Navigate<SelectDurationFormatViewModel, DurationFormat, DurationFormat>(durationFormat);
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheStoredPreferences()
            {
                var oldDurationFormat = DurationFormat.Decimal;
                var newDurationFormat = DurationFormat.Improved;
                var preferences = new MockPreferences { DurationFormat = oldDurationFormat };
                PreferencesSubject.OnNext(preferences);
                NavigationService
                    .Navigate<SelectDurationFormatViewModel, DurationFormat, DurationFormat>(Arg.Any<DurationFormat>())
                    .Returns(Task.FromResult(newDurationFormat));

                ViewModel.SelectDurationFormat.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .UpdatePreferences(Arg.Is<EditPreferencesDTO>(dto => dto.DurationFormat.Equals(New<DurationFormat>.Value(newDurationFormat))))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task SelectDurationFormatCommandCallsPushSync()
            {
                var oldDurationFormat = DurationFormat.Decimal;
                var newDurationFormat = DurationFormat.Improved;
                var preferences = new MockPreferences { DurationFormat = oldDurationFormat };
                PreferencesSubject.OnNext(preferences);
                NavigationService
                    .Navigate<SelectDurationFormatViewModel, DurationFormat, DurationFormat>(Arg.Any<DurationFormat>())
                    .Returns(Task.FromResult(newDurationFormat));
                var syncManager = Substitute.For<ISyncManager>();
                DataSource.SyncManager.Returns(syncManager);

                ViewModel.SelectDurationFormat.Execute();
                TestScheduler.Start();

                await syncManager.Received().PushSync();
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheDurationFormatProperty()
            {
                var oldDurationFormat = DurationFormat.Decimal;
                var newDurationFormat = DurationFormat.Improved;
                var oldPreferences = new MockPreferences { DurationFormat = oldDurationFormat };
                var newPreferences = new MockPreferences { DurationFormat = newDurationFormat };
                PreferencesSubject.OnNext(oldPreferences);
                NavigationService
                    .Navigate<SelectDurationFormatViewModel, DurationFormat, DurationFormat>(Arg.Any<DurationFormat>())
                    .Returns(Task.FromResult(newDurationFormat));
                InteractorFactory
                    .UpdatePreferences(Arg.Any<EditPreferencesDTO>())
                    .Execute()
                    .Returns(Observable.Return(newPreferences));

                ViewModel.SelectDurationFormat.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .UpdatePreferences(Arg.Is<EditPreferencesDTO>(dto => dto.DurationFormat.ValueOr(oldDurationFormat) == newDurationFormat))
                    .Received()
                    .Execute();
            }
        }

        public sealed class TheSelectBeginningOfWeekMethod : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToSelectBeginningOfWeekViewModelPassingCurrentBeginningOfWeek()
            {
                var beginningOfWeek = BeginningOfWeek.Friday;
                var user = new MockUser { BeginningOfWeek = beginningOfWeek };
                UserSubject.OnNext(user);

                ViewModel.SelectBeginningOfWeek.Execute();
                TestScheduler.Start();

                await NavigationService
                    .Received()
                    .Navigate<SelectBeginningOfWeekViewModel, BeginningOfWeek, BeginningOfWeek>(beginningOfWeek);
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheStoredPreferences()
            {
                var oldBeginningOfWeek = BeginningOfWeek.Tuesday;
                var newBeginningOfWeek = BeginningOfWeek.Sunday;

                var user = Substitute.For<IThreadSafeUser>();
                user.BeginningOfWeek.Returns(oldBeginningOfWeek);
                UserSubject.OnNext(user);
                NavigationService
                    .Navigate<SelectBeginningOfWeekViewModel, BeginningOfWeek, BeginningOfWeek>(Arg.Any<BeginningOfWeek>())
                    .Returns(Task.FromResult(newBeginningOfWeek));

                ViewModel.SelectBeginningOfWeek.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .UpdateUser(Arg.Is<EditUserDTO>(dto => dto.BeginningOfWeek == newBeginningOfWeek))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task SelectBeginningOfWeekCommandCallsPushSync()
            {
                var oldBeginningOfWeek = BeginningOfWeek.Tuesday;
                var newBeginningOfWeek = BeginningOfWeek.Sunday;
                var user = new MockUser { BeginningOfWeek = oldBeginningOfWeek };
                UserSubject.OnNext(user);
                NavigationService
                    .Navigate<SelectBeginningOfWeekViewModel, BeginningOfWeek, BeginningOfWeek>(Arg.Any<BeginningOfWeek>())
                    .Returns(Task.FromResult(newBeginningOfWeek));
                var syncManager = Substitute.For<ISyncManager>();
                DataSource.SyncManager.Returns(syncManager);

                ViewModel.SelectBeginningOfWeek.Execute();
                TestScheduler.Start();

                await syncManager.Received().PushSync();
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheBeginningOfWeekProperty()
            {
                var oldBeginningOfWeek = BeginningOfWeek.Tuesday;
                var newBeginningOfWeek = BeginningOfWeek.Sunday;
                var oldUser = new MockUser { BeginningOfWeek = oldBeginningOfWeek };
                var newUser = new MockUser { BeginningOfWeek = newBeginningOfWeek };
                UserSubject.OnNext(oldUser);
                NavigationService
                    .Navigate<SelectBeginningOfWeekViewModel, BeginningOfWeek, BeginningOfWeek>(Arg.Any<BeginningOfWeek>())
                    .Returns(Task.FromResult(newBeginningOfWeek));
                InteractorFactory
                    .UpdateUser(Arg.Any<EditUserDTO>())
                    .Execute()
                    .Returns(Observable.Return(newUser));

                ViewModel.SelectBeginningOfWeek.Execute();
                TestScheduler.Start();

                await InteractorFactory.UpdateUser(
                    Arg.Is<EditUserDTO>(dto => dto.BeginningOfWeek == newBeginningOfWeek
                )).Received().Execute();
            }
        }

        public sealed class TheOpenAboutViewMethod : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheAboutPage()
            {
                ViewModel.OpenAboutView.Execute();

                NavigationService.Received().Navigate<AboutViewModel>();
            }
        }

        public sealed class TheIsFeedBackSuccessViewShowingProperty : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void EmitsTrueWhenTapOnTheView()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsFeedbackSuccessViewShowing.StartWith(true).Subscribe(observer);
                viewModel.CloseFeedbackSuccessView();
                observer.Messages.Last().Value.Value.Should().BeFalse();
            }
        }

        public sealed class TheSubmitFeedbackUsingEmailMethod : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task CallsTheFeedbackService()
            {
                await ViewModel.SubmitFeedbackUsingEmail();
                await FeedbackService.Received().SubmitFeedback();
            }
        }

        public sealed class TheOpenCalendarSettingsAction : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToCalendarSettingsViewModel()
            {
                ViewModel.OpenCalendarSettings.Execute(Unit.Default);

                NavigationService.Received().Navigate<CalendarSettingsViewModel>();
            }
        }
    }
}
