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
	[Register ("SelectUserCalendarsViewController")]
	partial class SelectUserCalendarsViewController
	{
		[Outlet]
		UIKit.UILabel DescriptionLabel { get; set; }

		[Outlet]
		UIKit.UIButton DoneButton { get; set; }

		[Outlet]
		UIKit.UILabel HeadingLabel { get; set; }

		[Outlet]
		UIKit.UITableView TableView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (DoneButton != null) {
				DoneButton.Dispose ();
				DoneButton = null;
			}

			if (TableView != null) {
				TableView.Dispose ();
				TableView = null;
			}

			if (HeadingLabel != null) {
				HeadingLabel.Dispose ();
				HeadingLabel = null;
			}

			if (DescriptionLabel != null) {
				DescriptionLabel.Dispose ();
				DescriptionLabel = null;
			}
		}
	}
}
