using System;
using System.Reactive.Linq;
using CoreGraphics;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [MvxRootPresentation(WrapInNavigationController = true)]
    public sealed partial class OnboardingViewController : ReactiveViewController<OnboardingViewModel>, IUIScrollViewDelegate
    {
        private readonly TrackPage trackPagePlaceholder = TrackPage.Create();
        private readonly MostUsedPage mostUsedPagePlaceholder = MostUsedPage.Create();
        private readonly ReportsPage reportsPagePlaceholder = ReportsPage.Create();

        public OnboardingViewController()
            : base(nameof(OnboardingViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            preparePlaceholders();

            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                var navigationBarHeight = NavigationController.NavigationBar.Frame.Height;
                AdditionalSafeAreaInsets = new UIEdgeInsets(-navigationBarHeight, 0, 0, 0);
            }

            PageControl.Pages = ViewModel.NumberOfPages;
            FirstPageLabel.Text = Resources.OnboardingTrackPageCopy;
            SecondPageLabel.Text = Resources.OnboardingMostUsedPageCopy;
            ThirdPageLabel.Text = Resources.OnboardingReportsPageCopy;

            Skip.Rx()
                .BindAction(ViewModel.SkipOnboarding)
                .DisposedBy(DisposeBag);

            Next.Rx()
                .BindAction(ViewModel.GoToNextPage)
                .DisposedBy(DisposeBag);

            Previous.Rx()
                .BindAction(ViewModel.GoToPreviousPage)
                .DisposedBy(DisposeBag);

            ScrollView.Rx()
                .DecelerationEnded()
                .Select(_ => (int)(ScrollView.ContentOffset.X / ScrollView.Frame.Width))
                .Subscribe(newPage => ViewModel?.ChangePage(newPage))
                .DisposedBy(DisposeBag);

            ViewModel.BackgroundColor
                .Select(color => color.ToNativeColor())
                .Subscribe(View.Rx().AnimatedBackgroundColor())
                .DisposedBy(DisposeBag);

            ViewModel.BorderColor
                .Select(color => color.ToNativeColor())
                .Subscribe(PhoneFrame.Rx().AnimatedBackgroundColor())
                .DisposedBy(DisposeBag);
            
            ViewModel.CurrentPage
                .Select(backgroundImageForPage)
                .Subscribe(image => BackgroundImage.Image = image)
                .DisposedBy(DisposeBag);

            ViewModel.IsLastPage
                .Invert()
                .Subscribe(Skip.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsFirstPage
                .Invert()
                .Subscribe(Previous.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsTrackPage
                .Subscribe(trackPagePlaceholder.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsReportPage
                .Subscribe(reportsPagePlaceholder.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsSummaryPage
                .Subscribe(mostUsedPagePlaceholder.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.CurrentPage
                .Subscribe(p => PageControl.CurrentPage = p)
                .DisposedBy(DisposeBag);

            ViewModel.CurrentPage
                .Subscribe(p =>
                {
                    var scrollPoint = new CGPoint(ScrollView.Frame.Size.Width * p, 0);
                    ScrollView.SetContentOffset(scrollPoint, true);
                })
                .DisposedBy(DisposeBag);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            NavigationController.NavigationBar.UserInteractionEnabled = false;
            NavigationController.NavigationBarHidden = true;
        }

        public override bool PrefersStatusBarHidden()
            => true;

        private void preparePlaceholders()
        {
            PhoneContents.AddSubview(trackPagePlaceholder);
            PhoneContents.AddSubview(mostUsedPagePlaceholder);
            PhoneContents.AddSubview(reportsPagePlaceholder);
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (trackPagePlaceholder != null)
                trackPagePlaceholder.Frame = PhoneContents.Bounds;
            if (mostUsedPagePlaceholder != null)
                mostUsedPagePlaceholder.Frame = PhoneContents.Bounds;
            if (reportsPagePlaceholder != null)
                reportsPagePlaceholder.Frame = PhoneContents.Bounds;
        }

        private UIImage backgroundImageForPage(int page)
        {
            switch (page)
            {
                case OnboardingViewModel.TrackPage:
                    return UIImage.FromBundle("bgNoiseBlue");
                case OnboardingViewModel.MostUsedPage:
                    return UIImage.FromBundle("bgNoisePurple");
                case OnboardingViewModel.ReportsPage:
                    return UIImage.FromBundle("bgNoiseYellow");
                default:
                    return UIImage.FromBundle("bgNoiseYellow");
            }
        }

    }
}
