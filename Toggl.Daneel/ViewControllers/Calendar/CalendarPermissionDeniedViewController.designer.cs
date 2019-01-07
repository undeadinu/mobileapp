// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.ViewControllers.Calendar
{
	[Register ("CalendarPermissionDeniedViewController")]
	partial class CalendarPermissionDeniedViewController
	{
		[Outlet]
		UIKit.UIButton ContinueWithoutAccessButton { get; set; }

		[Outlet]
		UIKit.UIButton EnableAccessButton { get; set; }

		[Outlet]
		UIKit.UILabel HeadingLabel { get; set; }

		[Outlet]
		UIKit.UILabel MessageLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ContinueWithoutAccessButton != null) {
				ContinueWithoutAccessButton.Dispose ();
				ContinueWithoutAccessButton = null;
			}

			if (EnableAccessButton != null) {
				EnableAccessButton.Dispose ();
				EnableAccessButton = null;
			}

			if (HeadingLabel != null) {
				HeadingLabel.Dispose ();
				HeadingLabel = null;
			}

			if (MessageLabel != null) {
				MessageLabel.Dispose ();
				MessageLabel = null;
			}
		}
	}
}
