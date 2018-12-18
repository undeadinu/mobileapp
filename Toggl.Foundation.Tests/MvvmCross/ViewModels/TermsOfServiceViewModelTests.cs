using System;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class TermsOfServiceViewModelTests
    {
        public abstract class TermsOfServiceViewModelTest
            : BaseViewModelTests<TermsOfServiceViewModel>
        {
            protected override TermsOfServiceViewModel CreateViewModel()
                => new TermsOfServiceViewModel(BrowserService, RxActionFactory);
        }

        public sealed class TheConstructor : TermsOfServiceViewModelTest
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
                    () => new TermsOfServiceViewModel(browserService, rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheViewTermsOfServiceCommand : TermsOfServiceViewModelTest
        {
            private const string termsOfServiceUrl = "https://toggl.com/legal/terms/";

            [Fact, LogIfTooSlow]
            public async void OpensTermsOfService()
            {
                ViewModel.ViewTermsOfService.Execute();
                TestScheduler.Start();

                BrowserService.Received().OpenUrl(termsOfServiceUrl);
            }
        }

        public sealed class TheViewPrivacyPolicyCommand : TermsOfServiceViewModelTest
        {
            private const string privacyPolicyUrl = "https://toggl.com/legal/privacy/";

            [Fact, LogIfTooSlow]
            public async void OpensPrivacyPolicy()
            {
                ViewModel.ViewPrivacyPolicy.Execute();
                TestScheduler.Start();

                BrowserService.Received().OpenUrl(privacyPolicyUrl);
            }
        }
    }
}
