using System;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class BrowserViewModelTests
    {
        public abstract class BrowserViewModelTest : BaseViewModelTests<BrowserViewModel>
        {
            protected override BrowserViewModel CreateViewModel()
                => new BrowserViewModel(NavigationService, RxActionFactory);
        }

        public sealed class TheConstructor : BrowserViewModelTest
        {

            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useNavigationService,
                bool useRxActionFactory)
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new BrowserViewModel(
                        useNavigationService ? NavigationService : null,
                        useRxActionFactory ? RxActionFactory : null
                    );

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class ThePrepareMethod : BrowserViewModelTest
        {
            private readonly BrowserParameters parameters = new BrowserParameters
            {
                Title = "Some Title",
                Url = "https://someurl.com"
            };

            [Fact, LogIfTooSlow]
            public void SetsTheUrlProperty()
            {
                ViewModel.Prepare(parameters);

                ViewModel.Url.Should().Be(parameters.Url);
            }

            [Fact, LogIfTooSlow]
            public void SetsTheTitleProperty()
            {
                ViewModel.Prepare(parameters);

                ViewModel.Title.Should().Be(parameters.Title);
            }
        }
    }
}
