using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Extensions;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Xunit;
using static Toggl.Multivac.Extensions.EmailExtensions;
using static Toggl.Multivac.Extensions.PasswordExtensions;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public class TokenResetViewModelTests
    {
        public abstract class TokenResetViewModelTest : BaseViewModelTests<TokenResetViewModel>
        {
            protected static readonly Email ValidEmail = "susancalvin@psychohistorian.museum".ToEmail();
            protected static readonly Email InvalidEmail = "foo@".ToEmail();

            protected static readonly Password ValidPassword = "123456".ToPassword();
            protected static readonly Password InvalidPassword = Password.Empty;

            protected ITestableObserver<T> Observe<T>(IObservable<T> observable)
            {
                var observer = TestScheduler.CreateObserver<T>();
                observable.Subscribe(observer);
                return observer;
            }

            protected override TokenResetViewModel CreateViewModel()
                => new TokenResetViewModel(
                    UserAccessManager,
                    DataSource,
                    DialogService,
                    NavigationService,
                    UserPreferences,
                    AnalyticsService,
                    SchedulerProvider,
                    RxActionFactory,
                    InteractorFactory);
        }

        public sealed class TheConstructor : TokenResetViewModelTest
        {
            [Theory, LogIfTooSlow, ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useUserAccessManager,
                bool userNavigationService,
                bool useDataSource,
                bool useDialogService,
                bool useUserPreferences,
                bool useAnalyticsService,
                bool useSchedulerProvider,
                bool useRxActionFactory,
                bool useInteractorFactory
            )
            {
                var userAccessManager = useUserAccessManager ? UserAccessManager : null;
                var navigationService = userNavigationService ? NavigationService : null;
                var dataSource = useDataSource ? DataSource : null;
                var dialogService = useDialogService ? DialogService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new TokenResetViewModel(
                        userAccessManager,
                        dataSource,
                        dialogService,
                        navigationService,
                        userPreferences,
                        analyticsService,
                        schedulerProvider,
                        rxActionFactory,
                        interactorFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheNextIsEnabledProperty : TokenResetViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsFalseWhenThePasswordIsNotValid()
            {
                var nextIsEnabledObserver = Observe(ViewModel.NextIsEnabled);

                ViewModel.Password.OnNext(InvalidPassword.ToString());

                TestScheduler.Start();
                nextIsEnabledObserver.LastValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsTrueIfThePasswordIsValid()
            {
                var nextIsEnabledObserver = Observe(ViewModel.NextIsEnabled);

                ViewModel.Password.OnNext(ValidPassword.ToString());

                TestScheduler.Start();
                nextIsEnabledObserver.LastValue().Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsFalseWheThePasswordIsValidButTheViewIsLoading()
            {
                var scheduler = new TestScheduler();
                var never = Observable.Never<ITogglDataSource>();
                UserAccessManager.RefreshToken(Arg.Any<Password>()).Returns(never);
                ViewModel.Password.OnNext(ValidPassword.ToString());
                var nextIsEnabledObserver = Observe(ViewModel.NextIsEnabled);

                ViewModel.Done.Execute();

                TestScheduler.Start();
                nextIsEnabledObserver.LastValue().Should().BeFalse();
            }
        }

        public sealed class TheSetPasswordCommand : TokenResetViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void IsSuccessfullyEmmited()
            {
                var passwordObserver = Observe(ViewModel.Password.Select(Password.From));
                ViewModel.Password.OnNext(ValidPassword.ToString());

                TestScheduler.Start();
                passwordObserver.LastValue().Should().Be(ValidPassword);
            }
        }

        public sealed class TheDoneCommand : TokenResetViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task DoesNotAttemptToLoginWhileThePasswordIsNotValid()
            {
                ViewModel.Password.OnNext(InvalidPassword.ToString());
                var executionObserver = TestScheduler.CreateObserver<Unit>();

                ViewModel.Done.Execute().Subscribe(executionObserver);

                TestScheduler.Start();
                executionObserver.Messages.Last().Value.Kind.Should().Be(NotificationKind.OnError);
                await UserAccessManager.DidNotReceive().RefreshToken(Arg.Any<Password>());
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheUserAccessManagerWhenThePasswordIsValid()
            {
                ViewModel.Password.OnNext(ValidPassword.ToString());

                ViewModel.Done.Execute();

                TestScheduler.Start();
                await UserAccessManager.Received().RefreshToken(Arg.Is(ValidPassword));
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheMainViewModelModelWhenTheTokenRefreshSucceeds()
            {
                ViewModel.Password.OnNext(ValidPassword.ToString());
                UserAccessManager.RefreshToken(Arg.Any<Password>())
                            .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                ViewModel.Done.Execute();

                TestScheduler.Start();
                await NavigationService.Received().ForkNavigate<MainTabBarViewModel, MainViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task StopsTheViewModelLoadStateWhenItCompletes()
            {
                await ViewModel.Initialize();
                ViewModel.Password.OnNext(ValidPassword.ToString());
                var isLoadingObserver = Observe(ViewModel.Done.Executing);

                UserAccessManager.RefreshToken(Arg.Any<Password>())
                            .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                ViewModel.Done.Execute();
                TestScheduler.Start();

                isLoadingObserver.LastValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void StopsTheViewModelLoadStateWhenItErrors()
            {
                ViewModel.Password.OnNext(ValidPassword.ToString());
                UserAccessManager.RefreshToken(Arg.Any<Password>())
                            .Returns(Observable.Throw<ITogglDataSource>(new Exception()));
                var isLoadingObserver = Observe(ViewModel.Done.Executing);

                ViewModel.Done.Execute();

                TestScheduler.Start();
                var messages = isLoadingObserver.Messages.ToList();
                isLoadingObserver.LastValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotNavigateWhenTheLoginFails()
            {
                ViewModel.Password.OnNext(ValidPassword.ToString());
                UserAccessManager.RefreshToken(Arg.Any<Password>())
                            .Returns(Observable.Throw<ITogglDataSource>(new Exception()));

                ViewModel.Done.Execute();

                TestScheduler.Start();
                await NavigationService.DidNotReceive().Navigate<MainViewModel>();
            }
        }

        public sealed class TheSignOutCommand : TokenResetViewModelTest
        {
            private async Task setup(bool hasUnsyncedData = false, bool userConfirmsSignout = true)
            {
                DialogService.Confirm(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                             .Returns(Observable.Return(userConfirmsSignout));
                DataSource.HasUnsyncedData().Returns(Observable.Return(hasUnsyncedData));

                await ViewModel.Initialize();
            }

            [Fact, LogIfTooSlow]
            public async Task LogsTheUserOut()
            {
                await setup();

                ViewModel.SignOut.Execute();

                TestScheduler.Start();
                await InteractorFactory.Received().Logout(LogoutSource.TokenReset).Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheLoginViewModel()
            {
                await setup();

                ViewModel.SignOut.Execute();

                TestScheduler.Start();
                await NavigationService.Received().Navigate<LoginViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task AsksForPermissionIfThereIsUnsyncedData()
            {
                await setup(hasUnsyncedData: true);

                ViewModel.SignOut.Execute();

                TestScheduler.Start();
                await DialogService.Received().Confirm(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotLogTheUserOutIfPermissionIsDenied()
            {
                await setup(hasUnsyncedData: true, userConfirmsSignout: false);

                ViewModel.SignOut.Execute();

                TestScheduler.Start();
                InteractorFactory.DidNotReceive().Logout(Arg.Any<LogoutSource>());
            }
        }
    }
}
