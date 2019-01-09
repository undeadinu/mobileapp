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
	[Register ("NoWorkspaceViewController")]
	partial class NoWorkspaceViewController
	{
		[Outlet]
		Toggl.Daneel.Views.ActivityIndicatorView ActivityIndicatorView { get; set; }

		[Outlet]
		UIKit.UIButton CreateWorkspaceButton { get; set; }

		[Outlet]
		UIKit.UILabel HeadingLabel { get; set; }

		[Outlet]
		UIKit.UILabel TextLabel { get; set; }

		[Outlet]
		UIKit.UIButton TryAgainButton { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ActivityIndicatorView != null) {
				ActivityIndicatorView.Dispose ();
				ActivityIndicatorView = null;
			}

			if (CreateWorkspaceButton != null) {
				CreateWorkspaceButton.Dispose ();
				CreateWorkspaceButton = null;
			}

			if (TryAgainButton != null) {
				TryAgainButton.Dispose ();
				TryAgainButton = null;
			}

			if (HeadingLabel != null) {
				HeadingLabel.Dispose ();
				HeadingLabel = null;
			}

			if (TextLabel != null) {
				TextLabel.Dispose ();
				TextLabel = null;
			}
		}
	}
}
