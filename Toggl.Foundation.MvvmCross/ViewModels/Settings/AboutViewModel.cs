using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Multivac;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac.Extensions;
using System;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using Toggl.Foundation.Services;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class AboutViewModel : MvxViewModel
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IRxActionFactory rxActionFactory;

        public UIAction OpenPrivacyPolicyView { get; private set; }
        public UIAction OpenTermsOfServiceView { get; private set; }
        public UIAction OpenLicensesView { get; private set; }

        public AboutViewModel(IMvxNavigationService navigationService, IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.navigationService = navigationService;
            this.rxActionFactory = rxActionFactory;

            OpenPrivacyPolicyView = rxActionFactory.FromAsync(openPrivacyPolicyView);
            OpenTermsOfServiceView = rxActionFactory.FromAsync(openTermsOfServiceView);
            OpenLicensesView = rxActionFactory.FromAsync(openLicensesView);
        }

        private Task openPrivacyPolicyView()
            => navigationService
                .Navigate<BrowserViewModel, BrowserParameters>(
                    BrowserParameters.WithUrlAndTitle(Resources.PrivacyPolicyUrl, Resources.PrivacyPolicy));

        private Task openTermsOfServiceView()
            => navigationService
                .Navigate<BrowserViewModel, BrowserParameters>(
                    BrowserParameters.WithUrlAndTitle(Resources.TermsOfServiceUrl, Resources.TermsOfService));

        private Task openLicensesView()
            => navigationService
                .Navigate<LicensesViewModel>();
    }
}
