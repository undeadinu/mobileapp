using System;
using System.Reactive.Linq;
using MvvmCross.Binding;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using UIKit;
using static Toggl.Daneel.Extensions.ViewBindingExtensions;

namespace Toggl.Daneel.ViewControllers
{
    [MvxChildPresentation]
    public sealed partial class ForgotPasswordViewController
        : KeyboardAwareViewController<ForgotPasswordViewModel>
    {
        private const int distanceFromTop = 136;
        private const int backButtonFontSize = 14;
        private const int iPhoneSeScreenHeight = 568;
        private const int resetButtonBottomSpacing = 32;

        private bool viewInitialized;

        public ForgotPasswordViewController() : base(nameof(ForgotPasswordViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = Resources.LoginForgotPassword;
            ResetPasswordButton.SetTitle(Resources.GetPasswordResetLink, UIControlState.Normal);
            EmailTextField.Placeholder = Resources.EmailAddress;
            SuccessMessageLabel.Text = Resources.PasswordResetSuccess;

            prepareViews();

            //Text
            ViewModel.ErrorMessage
                .Subscribe(errorMessage =>
                {
                    ErrorLabel.Text = errorMessage;
                    ErrorLabel.Hidden = string.IsNullOrEmpty(errorMessage);
                })
                .DisposedBy(DisposeBag);

            EmailTextField.Rx().Text()
                .Select(Email.From)
                .Subscribe(ViewModel.Email.OnNext)
                .DisposedBy(DisposeBag);

            ViewModel.Reset.Executing
                .Subscribe(loading =>
                {
                    UIView.Transition(
                        ResetPasswordButton,
                        Animation.Timings.EnterTiming,
                        UIViewAnimationOptions.TransitionCrossDissolve,
                        () => ResetPasswordButton.SetTitle(loading ? "" : Resources.GetPasswordResetLink, UIControlState.Normal),
                        null
                    );
                })
                .DisposedBy(DisposeBag);

            //Visibility
            ViewModel.PasswordResetSuccessful
                .Subscribe(DoneCard.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);

            ViewModel.PasswordResetSuccessful
                .Invert()
                .Subscribe(ResetPasswordButton.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);

            ViewModel.PasswordResetSuccessful
                .Where(s => s == false)
                .Subscribe(_ => EmailTextField.BecomeFirstResponder())
                .DisposedBy(DisposeBag);

            ViewModel.Reset.Executing
                .Subscribe(ActivityIndicator.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);

            //Commands
            ResetPasswordButton.Rx()
                .BindAction(ViewModel.Reset)
                .DisposedBy(DisposeBag);
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (viewInitialized) return;

            viewInitialized = true;

            if (View.Frame.Height > iPhoneSeScreenHeight)
                TopConstraint.Constant = distanceFromTop;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            EmailTextField.BecomeFirstResponder();
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            ResetPasswordButtonBottomConstraint.Constant = e.FrameEnd.Height + resetButtonBottomSpacing;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            ResetPasswordButtonBottomConstraint.Constant = resetButtonBottomSpacing;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void prepareViews()
        {
            NavigationController.NavigationBarHidden = false;

            ResetPasswordButton.SetTitleColor(
                Color.Login.DisabledButtonColor.ToNativeColor(),
                UIControlState.Disabled
            );

            EmailTextField.Rx().ShouldReturn()
                .Subscribe(ViewModel.Reset.Inputs)
                .DisposedBy(DisposeBag);

            ActivityIndicator.StartSpinning();

            ErrorLabel.Hidden = true;

            prepareBackbutton();
        }

        private void prepareBackbutton()
        {
            var image = UIImage
                .FromBundle("icBackNoPadding")
                .ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            var color = Color.NavigationBar.BackButton.ToNativeColor();
            var backButton = new UIButton();
            backButton.TintColor = color;
            backButton.SetImage(image, UIControlState.Normal);
            backButton.SetTitleColor(color, UIControlState.Normal);
            backButton.SetTitle(Resources.Back, UIControlState.Normal);
            backButton.TitleLabel.Font = UIFont.SystemFontOfSize(backButtonFontSize, UIFontWeight.Medium);

            backButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            //Spacing between button image and title
            var spacing = 6;
            backButton.ImageEdgeInsets = new UIEdgeInsets(0, 0, 0, spacing);
            backButton.TitleEdgeInsets = new UIEdgeInsets(0, spacing, 0, 0);

            NavigationItem.HidesBackButton = true;
            NavigationItem.LeftItemsSupplementBackButton = false;
            NavigationItem.SetLeftBarButtonItem(new UIBarButtonItem(backButton), true);

            //Otherwise title gets clipped
            var frame = backButton.Frame;
            frame.Width = 90;
            backButton.Frame = frame;
            backButton.HorizontalAlignment = UIControlContentHorizontalAlignment.Left;
        }
    }
}
