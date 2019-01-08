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
	[Register ("EditDurationViewController")]
	partial class EditDurationViewController
	{
		[Outlet]
		UIKit.UIButton CloseButton { get; set; }

		[Outlet]
		UIKit.UIStackView ControlsStackView { get; set; }

		[Outlet]
		UIKit.UIDatePicker DatePicker { get; set; }

		[Outlet]
		UIKit.UIView DatePickerContainer { get; set; }

		[Outlet]
		Toggl.Daneel.Views.EditDuration.DurationField DurationInput { get; set; }

		[Outlet]
		UIKit.UILabel EndDateLabel { get; set; }

		[Outlet]
		UIKit.UILabel EndLabel { get; set; }

		[Outlet]
		UIKit.UILabel EndTimeLabel { get; set; }

		[Outlet]
		UIKit.UIView EndView { get; set; }

		[Outlet]
		UIKit.UIButton SaveButton { get; set; }

		[Outlet]
		UIKit.UIButton SetEndButton { get; set; }

		[Outlet]
		UIKit.UIStackView StackView { get; set; }

		[Outlet]
		UIKit.UILabel StartDateLabel { get; set; }

		[Outlet]
		UIKit.UILabel StartLabel { get; set; }

		[Outlet]
		UIKit.UILabel StartTimeLabel { get; set; }

		[Outlet]
		UIKit.UIView StartView { get; set; }

		[Outlet]
		UIKit.UILabel TitleLabel { get; set; }

		[Outlet]
		Toggl.Daneel.Views.EditDuration.WheelForegroundView WheelView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (CloseButton != null) {
				CloseButton.Dispose ();
				CloseButton = null;
			}

			if (ControlsStackView != null) {
				ControlsStackView.Dispose ();
				ControlsStackView = null;
			}

			if (DatePicker != null) {
				DatePicker.Dispose ();
				DatePicker = null;
			}

			if (DatePickerContainer != null) {
				DatePickerContainer.Dispose ();
				DatePickerContainer = null;
			}

			if (DurationInput != null) {
				DurationInput.Dispose ();
				DurationInput = null;
			}

			if (EndDateLabel != null) {
				EndDateLabel.Dispose ();
				EndDateLabel = null;
			}

			if (EndTimeLabel != null) {
				EndTimeLabel.Dispose ();
				EndTimeLabel = null;
			}

			if (EndView != null) {
				EndView.Dispose ();
				EndView = null;
			}

			if (SaveButton != null) {
				SaveButton.Dispose ();
				SaveButton = null;
			}

			if (SetEndButton != null) {
				SetEndButton.Dispose ();
				SetEndButton = null;
			}

			if (StackView != null) {
				StackView.Dispose ();
				StackView = null;
			}

			if (StartDateLabel != null) {
				StartDateLabel.Dispose ();
				StartDateLabel = null;
			}

			if (StartTimeLabel != null) {
				StartTimeLabel.Dispose ();
				StartTimeLabel = null;
			}

			if (StartView != null) {
				StartView.Dispose ();
				StartView = null;
			}

			if (WheelView != null) {
				WheelView.Dispose ();
				WheelView = null;
			}

			if (StartLabel != null) {
				StartLabel.Dispose ();
				StartLabel = null;
			}

			if (EndLabel != null) {
				EndLabel.Dispose ();
				EndLabel = null;
			}

			if (TitleLabel != null) {
				TitleLabel.Dispose ();
				TitleLabel = null;
			}
		}
	}
}
