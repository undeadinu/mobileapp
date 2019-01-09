// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.ViewControllers.Settings
{
	[Register ("SendFeedbackViewController")]
	partial class SendFeedbackViewController
	{
		[Outlet]
		UIKit.UIButton CloseButton { get; set; }

		[Outlet]
		UIKit.UILabel ErrorMessageLabel { get; set; }

		[Outlet]
		UIKit.UILabel ErrorTitleLabel { get; set; }

		[Outlet]
		UIKit.UIView ErrorView { get; set; }

		[Outlet]
		UIKit.UITextView FeedbackPlaceholderTextView { get; set; }

		[Outlet]
		UIKit.UITextView FeedbackTextView { get; set; }

		[Outlet]
		Toggl.Daneel.Views.ActivityIndicatorView IndicatorView { get; set; }

		[Outlet]
		UIKit.UIButton SendButton { get; set; }

		[Outlet]
		UIKit.UILabel TitleLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (CloseButton != null) {
				CloseButton.Dispose ();
				CloseButton = null;
			}

			if (ErrorView != null) {
				ErrorView.Dispose ();
				ErrorView = null;
			}

			if (FeedbackPlaceholderTextView != null) {
				FeedbackPlaceholderTextView.Dispose ();
				FeedbackPlaceholderTextView = null;
			}

			if (FeedbackTextView != null) {
				FeedbackTextView.Dispose ();
				FeedbackTextView = null;
			}

			if (IndicatorView != null) {
				IndicatorView.Dispose ();
				IndicatorView = null;
			}

			if (SendButton != null) {
				SendButton.Dispose ();
				SendButton = null;
			}

			if (TitleLabel != null) {
				TitleLabel.Dispose ();
				TitleLabel = null;
			}

			if (ErrorTitleLabel != null) {
				ErrorTitleLabel.Dispose ();
				ErrorTitleLabel = null;
			}

			if (ErrorMessageLabel != null) {
				ErrorMessageLabel.Dispose ();
				ErrorMessageLabel = null;
			}
		}
	}
}
