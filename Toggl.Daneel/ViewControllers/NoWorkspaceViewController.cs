using System;
using System.Reactive.Linq;
using CoreGraphics;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalDialogPresentation]
    public sealed partial class NoWorkspaceViewController
        : ReactiveViewController<NoWorkspaceViewModel>
    {
        private const float cardHeight = 368;

        public NoWorkspaceViewController() : base(nameof(NoWorkspaceViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            CreateWorkspaceButton.SetTitle(Resources.CreateNewWorkspace, UIControlState.Normal);
            HeadingLabel.Text = Resources.UhOh;
            TextLabel.Text = Resources.NoWorkspaceErrorMessage;

            var screenWidth = UIScreen.MainScreen.Bounds.Width;
            PreferredContentSize = new CGSize
            {
                // ScreenWidth - 32 for 16pt margins on both sides
                Width = screenWidth > 320 ? screenWidth - 32 : 312,
                Height = cardHeight
            };

            prepareViews();

            CreateWorkspaceButton.Rx()
                .BindAction(ViewModel.CreateWorkspaceWithDefaultName)
                .DisposedBy(DisposeBag);

            TryAgainButton.Rx()
                .BindAction(ViewModel.TryAgain)
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Invert()
                .Subscribe(CreateWorkspaceButton.Rx().Enabled())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Invert()
                .Subscribe(TryAgainButton.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading.StartWith(false)
                .Subscribe(ActivityIndicatorView.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            ActivityIndicatorView.StartSpinning();
        }

        private void prepareViews()
        {
            ActivityIndicatorView.IndicatorColor = Color.NoWorkspace.ActivityIndicator.ToNativeColor();
            CreateWorkspaceButton.SetTitleColor(Color.NoWorkspace.DisabledCreateWorkspaceButton.ToNativeColor(), UIControlState.Disabled);
        }
    }
}
