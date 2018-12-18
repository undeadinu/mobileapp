using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class CalendarPermissionDeniedViewModelTests
    {
        public abstract class CalendarPermissionDeniedViewModelTest
            : BaseViewModelTests<CalendarPermissionDeniedViewModel>
        {
            protected override CalendarPermissionDeniedViewModel CreateViewModel()
                => new CalendarPermissionDeniedViewModel(PermissionsService, RxActionFactory);
        }

        public sealed class TheConstructor : CalendarPermissionDeniedViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool usePermissionsService,
                bool useRxActionFactory)
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new CalendarPermissionDeniedViewModel(
                        usePermissionsService ? PermissionsService : null,
                        useRxActionFactory ? RxActionFactory : null
                    );

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheEnableAccessAction : CalendarPermissionDeniedViewModelTest
        {
            [Fact]
            public async Task OpensAppSettings()
            {
                ViewModel.EnableAccess.Execute();

                PermissionsService.Received().OpenAppSettings();
            }
        }
    }
}
