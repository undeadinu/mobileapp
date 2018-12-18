using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class OutdatedAppViewModel : MvxViewModel
    {
        private readonly IRxActionFactory rxActionFactory;

        public UIAction OpenWebsite { get; }

        public UIAction UpdateApp { get; }

        private const string togglWebsiteUrl = "https://toggl.com";

        private readonly IBrowserService browserService;

        public OutdatedAppViewModel(IBrowserService browserService, IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(browserService, nameof(browserService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.browserService = browserService;
            this.rxActionFactory = rxActionFactory;

            UpdateApp = rxActionFactory.FromAction(updateApp);
            OpenWebsite = rxActionFactory.FromAction(openWebsite);
        }

        private void openWebsite()
        {
            browserService.OpenUrl(togglWebsiteUrl);
        }

        private void updateApp()
        {
            browserService.OpenStore();
        }
    }
}
