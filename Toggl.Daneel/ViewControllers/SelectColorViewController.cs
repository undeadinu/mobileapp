using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using CoreGraphics;
using MvvmCross.Binding.BindingContext;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.Views;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalDialogPresentation]
    public sealed partial class SelectColorViewController : ReactiveViewController<SelectColorViewModel>
    {
        private const int customColorEnabledHeight = 365;
        private const int customColorDisabledHeight = 233;

        private ColorSelectionCollectionViewSource source;

        public SelectColorViewController()
            : base(nameof(SelectColorViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TitleLabel.Text = Resources.ProjectColor;
            CloseButton.SetTitle(Resources.Save, UIControlState.Normal);

            prepareViews();

            //Collection View
            ColorCollectionView.RegisterNibForCell(ColorSelectionViewCell.Nib, ColorSelectionViewCell.Identifier);
            source = new ColorSelectionCollectionViewSource(ViewModel.SelectableColors);
            ColorCollectionView.Source = source;
            ViewModel.SelectableColors
                .Subscribe(replaceColors)
                .DisposedBy(DisposeBag);

            source.ColorSelected
                .Subscribe(ViewModel.SelectColor.Inputs)
                .DisposedBy(DisposeBag);

            // Commands
            SaveButton.Rx()
                .BindAction(ViewModel.Save)
                .DisposedBy(DisposeBag);

            CloseButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            // Picker view
            PickerView.Rx().Hue()
                .Subscribe(ViewModel.SetHue.Inputs)
                .DisposedBy(DisposeBag);


            PickerView.Rx().Saturation()
                .Subscribe(ViewModel.SetSaturation.Inputs)
                .DisposedBy(DisposeBag);

            SliderView.Rx().Value()
                .Select(v => 1 - v)
                .Subscribe(ViewModel.SetValue.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.Hue
                .Subscribe(PickerView.Rx().HueObserver())
                .DisposedBy(DisposeBag);

            ViewModel.Saturation
                .Subscribe(PickerView.Rx().SaturationObserver())
                .DisposedBy(DisposeBag);

            ViewModel.Value
                .Subscribe(PickerView.Rx().ValueObserver())
                .DisposedBy(DisposeBag);
        }

        private void prepareViews()
        {
            var screenWidth = UIScreen.MainScreen.Bounds.Width;
            PreferredContentSize = new CGSize
            {
                // ScreenWidth - 32 for 16pt margins on both sides
                Width = screenWidth > 320 ? screenWidth - 32 : 312,
                Height = ViewModel.AllowCustomColors ? customColorEnabledHeight : customColorDisabledHeight
            };

            if (!ViewModel.AllowCustomColors)
            {
                SliderView.Hidden = true;
                PickerView.Hidden = true;
                SliderBackgroundView.Hidden = true;
                return;
            }

            // Initialize picker related layers
            var usableWidth = PreferredContentSize.Width - 28;
            PickerView.InitializeLayers(new CGRect(0, 0, usableWidth, 80));
            SliderBackgroundView.InitializeLayer(new CGRect(0, 0, usableWidth, 18));

            // Remove track
            SliderView.SetMinTrackImage(new UIImage(), UIControlState.Normal);
            SliderView.SetMaxTrackImage(new UIImage(), UIControlState.Normal);
        }

        private void replaceColors(IEnumerable<SelectableColorViewModel> colors)
        {
            source.SetNewColors(colors);
            ColorCollectionView.ReloadData();
        }
    }
}

