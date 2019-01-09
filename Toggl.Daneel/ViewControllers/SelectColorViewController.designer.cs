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
	[Register ("SelectColorViewController")]
	partial class SelectColorViewController
	{
		[Outlet]
		UIKit.UIButton CloseButton { get; set; }

		[Outlet]
		UIKit.UICollectionView ColorCollectionView { get; set; }

		[Outlet]
		Toggl.Daneel.Views.HueSaturationPickerView PickerView { get; set; }

		[Outlet]
		UIKit.UIButton SaveButton { get; set; }

		[Outlet]
		Toggl.Daneel.Views.ValueSliderView SliderBackgroundView { get; set; }

		[Outlet]
		UIKit.UISlider SliderView { get; set; }

		[Outlet]
		UIKit.UILabel TitleLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (CloseButton != null) {
				CloseButton.Dispose ();
				CloseButton = null;
			}

			if (ColorCollectionView != null) {
				ColorCollectionView.Dispose ();
				ColorCollectionView = null;
			}

			if (PickerView != null) {
				PickerView.Dispose ();
				PickerView = null;
			}

			if (SaveButton != null) {
				SaveButton.Dispose ();
				SaveButton = null;
			}

			if (SliderBackgroundView != null) {
				SliderBackgroundView.Dispose ();
				SliderBackgroundView = null;
			}

			if (SliderView != null) {
				SliderView.Dispose ();
				SliderView = null;
			}

			if (TitleLabel != null) {
				TitleLabel.Dispose ();
				TitleLabel = null;
			}
		}
	}
}
