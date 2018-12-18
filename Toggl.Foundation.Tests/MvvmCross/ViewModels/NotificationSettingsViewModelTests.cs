using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Settings;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class NotificationSettingsViewModelTests
    {
        public abstract class NotificationSettingsViewModelTest : BaseViewModelTests<NotificationSettingsViewModel>
        {
            protected override NotificationSettingsViewModel CreateViewModel()
                => new NotificationSettingsViewModel(NavigationService, BackgroundService, PermissionsService, UserPreferences, SchedulerProvider, RxActionFactory);
        }

        public sealed class TheConstructor : NotificationSettingsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useNavigationService,
                bool useBackgroundService,
                bool usePermissionsService,
                bool useUserPreferences,
                bool useSchedulerProvider,
                bool useRxActionFactory)
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new NotificationSettingsViewModel(
                        useNavigationService ? NavigationService : null,
                        useBackgroundService ? BackgroundService : null,
                        usePermissionsService ? PermissionsService : null,
                        useUserPreferences ? UserPreferences : null,
                        useSchedulerProvider ? SchedulerProvider : null,
                        useRxActionFactory ? RxActionFactory : null
                    );

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class ThePermissionGrantedProperty : NotificationSettingsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async Task GetsInitialisedToTheProperValue(bool permissionGranted)
            {
                var observer = TestScheduler.CreateObserver<bool>();
                PermissionsService.NotificationPermissionGranted.Returns(Observable.Return(permissionGranted));

                var viewModel = new NotificationSettingsViewModel(NavigationService, BackgroundService, PermissionsService, UserPreferences, SchedulerProvider, RxActionFactory);
                viewModel.PermissionGranted.Subscribe(observer);

                await viewModel.Initialize();
                TestScheduler.Start();

                observer.Messages.First().Value.Value.Should().Be(permissionGranted);
            }
        }

        public sealed class TheRequestAccessAction : NotificationSettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task OpensAppSettings()
            {
                ViewModel.RequestAccess.Execute(Unit.Default);
                TestScheduler.Start();

                PermissionsService.Received().OpenAppSettings();
            }
        }

        public sealed class TheOpenUpcomingEventsAction : NotificationSettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheUpcomingEvents()
            {
                ViewModel.OpenUpcomingEvents.Execute();
                TestScheduler.Start();

                NavigationService.Received().Navigate<UpcomingEventsNotificationSettingsViewModel, Unit>();
            }
        }
    }
}
