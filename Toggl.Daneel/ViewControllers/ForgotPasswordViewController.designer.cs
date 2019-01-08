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
	[Register ("ForgotPasswordViewController")]
	partial class ForgotPasswordViewController
	{
		[Outlet]
		Toggl.Daneel.Views.ActivityIndicatorView ActivityIndicator { get; set; }

		[Outlet]
		UIKit.UIView DoneCard { get; set; }

		[Outlet]
		Toggl.Daneel.Views.LoginTextField EmailTextField { get; set; }

		[Outlet]
		UIKit.UILabel ErrorLabel { get; set; }

		[Outlet]
		UIKit.UIButton ResetPasswordButton { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint ResetPasswordButtonBottomConstraint { get; set; }

		[Outlet]
		UIKit.UILabel SuccessMessageLabel { get; set; }

		[Outlet]
		UIKit.NSLayoutConstraint TopConstraint { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ActivityIndicator != null) {
				ActivityIndicator.Dispose ();
				ActivityIndicator = null;
			}

			if (DoneCard != null) {
				DoneCard.Dispose ();
				DoneCard = null;
			}

			if (EmailTextField != null) {
				EmailTextField.Dispose ();
				EmailTextField = null;
			}

			if (ErrorLabel != null) {
				ErrorLabel.Dispose ();
				ErrorLabel = null;
			}

			if (ResetPasswordButton != null) {
				ResetPasswordButton.Dispose ();
				ResetPasswordButton = null;
			}

			if (ResetPasswordButtonBottomConstraint != null) {
				ResetPasswordButtonBottomConstraint.Dispose ();
				ResetPasswordButtonBottomConstraint = null;
			}

			if (TopConstraint != null) {
				TopConstraint.Dispose ();
				TopConstraint = null;
			}

			if (SuccessMessageLabel != null) {
				SuccessMessageLabel.Dispose ();
				SuccessMessageLabel = null;
			}
		}
	}
}
