// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.ViewControllers
{
	[Register ("LoginView")]
	partial class LoginViewController
	{
		[Outlet]
		Toggl.Daneel.Views.ActivityIndicatorView ActivityIndicator { get; set; }

		[Outlet]
		UIKit.UILabel DontHaveAnAccountLabel { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint EmailFieldTopConstraint { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		Toggl.Daneel.Views.LoginTextField EmailTextField { get; set; }

		[Outlet]
		UIKit.UILabel ErrorLabel { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton ForgotPasswordButton { get; set; }

		[Outlet]
		UIKit.UIButton GoogleLoginButton { get; set; }

		[Outlet]
		UIKit.UIButton LoginButton { get; set; }

		[Outlet]
		UIKit.UIImageView LogoImageView { get; set; }

		[Outlet]
		UIKit.UILabel OrLabel { get; set; }

		[Outlet]
		UIKit.UIButton PasswordManagerButton { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		Toggl.Daneel.Views.LoginTextField PasswordTextField { get; set; }

		[Outlet]
		UIKit.UIButton ShowPasswordButton { get; set; }

		[Outlet]
		UIKit.UIView SignupCard { get; set; }

		[Outlet]
		UIKit.UILabel SignUpForFreeLabel { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint TopConstraint { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ActivityIndicator != null) {
				ActivityIndicator.Dispose ();
				ActivityIndicator = null;
			}

			if (EmailFieldTopConstraint != null) {
				EmailFieldTopConstraint.Dispose ();
				EmailFieldTopConstraint = null;
			}

			if (EmailTextField != null) {
				EmailTextField.Dispose ();
				EmailTextField = null;
			}

			if (ErrorLabel != null) {
				ErrorLabel.Dispose ();
				ErrorLabel = null;
			}

			if (ForgotPasswordButton != null) {
				ForgotPasswordButton.Dispose ();
				ForgotPasswordButton = null;
			}

			if (GoogleLoginButton != null) {
				GoogleLoginButton.Dispose ();
				GoogleLoginButton = null;
			}

			if (LoginButton != null) {
				LoginButton.Dispose ();
				LoginButton = null;
			}

			if (LogoImageView != null) {
				LogoImageView.Dispose ();
				LogoImageView = null;
			}

			if (PasswordManagerButton != null) {
				PasswordManagerButton.Dispose ();
				PasswordManagerButton = null;
			}

			if (PasswordTextField != null) {
				PasswordTextField.Dispose ();
				PasswordTextField = null;
			}

			if (ShowPasswordButton != null) {
				ShowPasswordButton.Dispose ();
				ShowPasswordButton = null;
			}

			if (SignupCard != null) {
				SignupCard.Dispose ();
				SignupCard = null;
			}

			if (TopConstraint != null) {
				TopConstraint.Dispose ();
				TopConstraint = null;
			}

			if (OrLabel != null) {
				OrLabel.Dispose ();
				OrLabel = null;
			}

			if (DontHaveAnAccountLabel != null) {
				DontHaveAnAccountLabel.Dispose ();
				DontHaveAnAccountLabel = null;
			}

			if (SignUpForFreeLabel != null) {
				SignUpForFreeLabel.Dispose ();
				SignUpForFreeLabel = null;
			}
		}
	}
}
