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
	[Register ("NotificationSettingsViewController")]
	partial class NotificationSettingsViewController
	{
		[Outlet]
		UIKit.UIStackView CalendarNotificationsContainer { get; set; }

		[Outlet]
		UIKit.UIView CalendarNotificationsRow { get; set; }

		[Outlet]
		UIKit.UILabel CalendarNotificationsValue { get; set; }

		[Outlet]
		UIKit.UILabel ExplainationLabel { get; set; }

		[Outlet]
		UIKit.UILabel NotificationDisabledLabel { get; set; }

		[Outlet]
		UIKit.UIButton OpenSettingsButton { get; set; }

		[Outlet]
		UIKit.UIStackView OpenSettingsContainer { get; set; }

		[Outlet]
		UIKit.UILabel RowLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (CalendarNotificationsContainer != null) {
				CalendarNotificationsContainer.Dispose ();
				CalendarNotificationsContainer = null;
			}

			if (CalendarNotificationsRow != null) {
				CalendarNotificationsRow.Dispose ();
				CalendarNotificationsRow = null;
			}

			if (CalendarNotificationsValue != null) {
				CalendarNotificationsValue.Dispose ();
				CalendarNotificationsValue = null;
			}

			if (OpenSettingsButton != null) {
				OpenSettingsButton.Dispose ();
				OpenSettingsButton = null;
			}

			if (OpenSettingsContainer != null) {
				OpenSettingsContainer.Dispose ();
				OpenSettingsContainer = null;
			}

			if (NotificationDisabledLabel != null) {
				NotificationDisabledLabel.Dispose ();
				NotificationDisabledLabel = null;
			}

			if (ExplainationLabel != null) {
				ExplainationLabel.Dispose ();
				ExplainationLabel = null;
			}

			if (RowLabel != null) {
				RowLabel.Dispose ();
				RowLabel = null;
			}
		}
	}
}
