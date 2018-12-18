using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class OutdatedAppViewModelTests
    {
        public abstract class OutdatedAppViewModelTest : BaseViewModelTests<OutdatedAppViewModel>
        {
            protected override OutdatedAppViewModel CreateViewModel()
                => new OutdatedAppViewModel(BrowserService, RxActionFactory);
        }

        public sealed class TheConstructor : OutdatedAppViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useBrowserService,
                bool useRxActionFactory)
            {
                var browserService = useBrowserService ? BrowserService : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new OutdatedAppViewModel(
                        browserService, rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheUpdateAppAction : OutdatedAppViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task CallsTheOpenStoreMethodOfTheBrowserService()
            {
                ViewModel.UpdateApp.Execute();
                TestScheduler.Start();

                BrowserService.Received().OpenStore();
            }
        }

        public sealed class TheOpenWebsiteCommand : OutdatedAppViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task CallsTheOpenWebsiteMethodOfTheBrowserService()
            {
                const string togglWebsiteUrl = "https://toggl.com";

                ViewModel.OpenWebsite.Execute();
                TestScheduler.Start();

                BrowserService.Received().OpenUrl(Arg.Is(togglWebsiteUrl));
            }
        }
    }
}
