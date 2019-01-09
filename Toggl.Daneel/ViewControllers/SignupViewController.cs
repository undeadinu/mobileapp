using System;
using System.Linq;
using System.Reactive.Linq;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using UIKit;
using static Toggl.Daneel.Extensions.LoginSignupViewExtensions;
using static Toggl.Daneel.Extensions.ViewExtensions;


namespace Toggl.Daneel.ViewControllers
{
    [MvxRootPresentation(WrapInNavigationController = true)]
    public sealed partial class SignupViewController : ReactiveViewController<SignupViewModel>
    {
        private const int iPhoneSeScreenHeight = 568;

        private bool keyboardIsOpen = false;

        private const int topConstraintForBiggerScreens = 72;
        private const int topConstraintForBiggerScreensWithKeyboard = 40;

        private const int emailTopConstraint = 42;
        private const int emailTopConstraintWithKeyboard = 12;

        public SignupViewController() : base(nameof(SignupViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            EmailTextField.Placeholder = Resources.EmailAddress;
            PasswordTextField.Placeholder = Resources.Password;
            SignUpCardTitleLabel.Text = Resources.AlreadyHaveAnAccountQuestionMark;
            SignUpCardLoginLabel.Text = Resources.LoginTitle;
            OrLabel.Text = Resources.Or.ToUpper();
            GoogleSignupButton.SetTitle(Resources.GoogleSignUp, UIControlState.Normal);

            NavigationController.NavigationBarHidden = true;

            UIKeyboard.Notifications.ObserveWillShow(KeyboardWillShow);
            UIKeyboard.Notifications.ObserveWillHide(KeyboardWillHide);

            //Text
            ViewModel.ErrorMessage
                .Subscribe(ErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.Email
                .Subscribe(EmailTextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.Password
                .Subscribe(PasswordTextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            EmailTextField.Rx().Text()
                .Select(Email.From)
                .Subscribe(ViewModel.SetEmail)
                .DisposedBy(DisposeBag);

            PasswordTextField.Rx().Text()
                .Select(Password.From)
                .Subscribe(ViewModel.SetPassword)
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(signupButtonTitle)
                .Subscribe(SignupButton.Rx().AnimatedTitle())
                .DisposedBy(DisposeBag);

            ViewModel.CountryButtonTitle
                .Subscribe(SelectCountryButton.Rx().AnimatedTitle())
                .DisposedBy(DisposeBag);

            //Visibility
            ViewModel.HasError
                .Subscribe(ErrorLabel.Rx().AnimatedIsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Subscribe(ActivityIndicator.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);

            ViewModel.IsPasswordMasked
                .Skip(1)
                .Subscribe(PasswordTextField.Rx().SecureTextEntry())
                .DisposedBy(DisposeBag);

            ViewModel.IsShowPasswordButtonVisible
                .Subscribe(ShowPasswordButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            PasswordTextField.FirstResponder
                .Subscribe(ViewModel.SetIsShowPasswordButtonVisible)
                .DisposedBy(DisposeBag);

            ViewModel.IsCountryErrorVisible
                .Subscribe(CountryNotSelectedImageView.Rx().AnimatedIsVisible())
                .DisposedBy(DisposeBag);

            //Commands
            LoginCard.Rx()
                .BindAction(ViewModel.Login)
                .DisposedBy(DisposeBag);

            SignupButton.Rx()
                .BindAction(ViewModel.Signup)
                .DisposedBy(DisposeBag);

            GoogleSignupButton.Rx()
                .BindAction(ViewModel.GoogleSignup)
                .DisposedBy(DisposeBag);

            ShowPasswordButton.Rx().Tap()
                .Subscribe(ViewModel.TogglePasswordVisibility)
                .DisposedBy(DisposeBag);

            SelectCountryButton.Rx()
                .BindAction(ViewModel.PickCountry)
                .DisposedBy(DisposeBag);

            //Color
            ViewModel.HasError
                .Select(signupButtonTintColor)
                .Subscribe(SignupButton.Rx().TintColor())
                .DisposedBy(DisposeBag);

            ViewModel.SignupEnabled
                .Select(signupButtonTitleColor)
                .Subscribe(SignupButton.Rx().TitleColor())
                .DisposedBy(DisposeBag);

            //Animation
            ViewModel.Shake
                .Subscribe(shakeTargets =>
                {
                    if (shakeTargets.HasFlag(SignupViewModel.ShakeTargets.Email))
                        EmailTextField.Shake();

                    if (shakeTargets.HasFlag(SignupViewModel.ShakeTargets.Password))
                        PasswordTextField.Shake();

                    if (shakeTargets.HasFlag(SignupViewModel.ShakeTargets.Country))
                        SelectCountryButton.Shake();
                })
                .DisposedBy(DisposeBag);

            prepareViews();

            UIColor signupButtonTintColor(bool hasError)
                => hasError ? UIColor.White : UIColor.Black;

            UIColor signupButtonTitleColor(bool enabled) => enabled
                ? Color.Login.EnabledButtonColor.ToNativeColor()
                : Color.Login.DisabledButtonColor.ToNativeColor();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (View.Frame.Height > iPhoneSeScreenHeight && !keyboardIsOpen)
                TopConstraint.Constant = topConstraintForBiggerScreens;

            LoginCard.SetupBottomCard();
            GoogleSignupButton.SetupGoogleButton();
        }

        private void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            keyboardIsOpen = true;
            if (View.Frame.Height > iPhoneSeScreenHeight)
            {
                TopConstraint.Constant = topConstraintForBiggerScreensWithKeyboard;
            }
            else
            {
                EmailFieldTopConstraint.Constant = emailTopConstraintWithKeyboard;
            }
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            keyboardIsOpen = false;
            if (View.Frame.Height > iPhoneSeScreenHeight)
            {
                TopConstraint.Constant = topConstraintForBiggerScreens;
            }
            else
            {
                EmailFieldTopConstraint.Constant = emailTopConstraint;
            }
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void prepareViews()
        {
            NavigationController.NavigationBarHidden = true;

            ActivityIndicator.Alpha = 0;
            ActivityIndicator.StartSpinning();

            SignupButton.SetTitleColor(
                Color.Login.DisabledButtonColor.ToNativeColor(),
                UIControlState.Disabled
            );

            EmailTextField.ShouldReturn += _ => {
                PasswordTextField.BecomeFirstResponder();
                return false;
            };

            PasswordTextField.ShouldReturn += _ =>
            {
                ViewModel.Signup.Execute();
                PasswordTextField.ResignFirstResponder();
                return false;
            };

            View.AddGestureRecognizer(new UITapGestureRecognizer(() =>
            {
                EmailTextField.ResignFirstResponder();
                PasswordTextField.ResignFirstResponder();
            }));

            ShowPasswordButton.SetupShowPasswordButton();
        }

        private string signupButtonTitle(bool isLoading)
            => isLoading ? "" : Resources.SignUpTitle;
    }
}

