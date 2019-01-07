// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel
{
	[Register ("CalendarSettingsTableViewHeader")]
	partial class CalendarSettingsTableViewHeader
	{
		[Outlet]
		UIKit.UILabel CalendarPermissionStatusLabel { get; set; }

		[Outlet]
		UIKit.UILabel DescriptionLabel { get; set; }

		[Outlet]
		UIKit.UIView EnableCalendarAccessView { get; set; }

		[Outlet]
		UIKit.UILabel TextLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (CalendarPermissionStatusLabel != null) {
				CalendarPermissionStatusLabel.Dispose ();
				CalendarPermissionStatusLabel = null;
			}

			if (EnableCalendarAccessView != null) {
				EnableCalendarAccessView.Dispose ();
				EnableCalendarAccessView = null;
			}

			if (TextLabel != null) {
				TextLabel.Dispose ();
				TextLabel = null;
			}

			if (DescriptionLabel != null) {
				DescriptionLabel.Dispose ();
				DescriptionLabel = null;
			}
		}
	}
}
