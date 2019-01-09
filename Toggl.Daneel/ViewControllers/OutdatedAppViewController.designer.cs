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
	[Register ("OutdatedAppViewController")]
	partial class OutdatedAppViewController
	{
		[Outlet]
		UIKit.UILabel HeadingLabel { get; set; }

		[Outlet]
		UIKit.UILabel TextLabel { get; set; }

		[Outlet]
		UIKit.UIButton UpdateButton { get; set; }

		[Outlet]
		UIKit.UIButton WebsiteButton { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (UpdateButton != null) {
				UpdateButton.Dispose ();
				UpdateButton = null;
			}

			if (WebsiteButton != null) {
				WebsiteButton.Dispose ();
				WebsiteButton = null;
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
