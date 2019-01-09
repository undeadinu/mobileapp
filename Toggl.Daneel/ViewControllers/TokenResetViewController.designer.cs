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
	[Register ("TokenResetViewController")]
	partial class TokenResetViewController
	{
		[Outlet]
		Toggl.Daneel.Views.ActivityIndicatorView ActivityIndicatorView { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint BottomConstraint { get; set; }

		[Outlet]
		UIKit.UILabel EmailLabel { get; set; }

		[Outlet]
		UIKit.UILabel ErrorLabel { get; set; }

		[Outlet]
		UIKit.UIView ErrorView { get; set; }

		[Outlet]
		UIKit.UILabel InstructionLabel { get; set; }

		[Outlet]
		UIKit.UITextField PasswordTextField { get; set; }

		[Outlet]
		UIKit.UILabel ResetSuccessLabel { get; set; }

		[Outlet]
		UIKit.UIImageView ShowPasswordButton { get; set; }

		[Outlet]
		UIKit.UIButton SignOutButton { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ActivityIndicatorView != null) {
				ActivityIndicatorView.Dispose ();
				ActivityIndicatorView = null;
			}

			if (BottomConstraint != null) {
				BottomConstraint.Dispose ();
				BottomConstraint = null;
			}

			if (EmailLabel != null) {
				EmailLabel.Dispose ();
				EmailLabel = null;
			}

			if (ErrorLabel != null) {
				ErrorLabel.Dispose ();
				ErrorLabel = null;
			}

			if (ErrorView != null) {
				ErrorView.Dispose ();
				ErrorView = null;
			}

			if (PasswordTextField != null) {
				PasswordTextField.Dispose ();
				PasswordTextField = null;
			}

			if (ShowPasswordButton != null) {
				ShowPasswordButton.Dispose ();
				ShowPasswordButton = null;
			}

			if (SignOutButton != null) {
				SignOutButton.Dispose ();
				SignOutButton = null;
			}

			if (InstructionLabel != null) {
				InstructionLabel.Dispose ();
				InstructionLabel = null;
			}

			if (ResetSuccessLabel != null) {
				ResetSuccessLabel.Dispose ();
				ResetSuccessLabel = null;
			}
		}
	}
}
