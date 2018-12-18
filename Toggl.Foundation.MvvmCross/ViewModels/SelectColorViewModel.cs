using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using MvvmCross.UI;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public class SelectColorViewModel : MvxViewModel<ColorParameters, MvxColor>
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IRxActionFactory rxActionFactory;

        private MvxColor defaultColor;
        private IObservable<MvxColor> customColor;
        private BehaviorSubject<MvxColor> selectedColor = new BehaviorSubject<MvxColor>(MvxColors.Transparent);

        private BehaviorSubject<float> hue { get; } = new BehaviorSubject<float>(0.0f);
        private BehaviorSubject<float> saturation { get; } = new BehaviorSubject<float>(0.0f);
        private BehaviorSubject<float> value { get; } = new BehaviorSubject<float>(0.375f);

        public bool AllowCustomColors { get; private set; }

        public IObservable<IEnumerable<SelectableColorViewModel>> SelectableColors { get; }
        public IObservable<float> Hue { get; }
        public IObservable<float> Saturation { get; }
        public IObservable<float> Value { get; }

        public UIAction Save { get; }
        public UIAction Close { get; }
        public InputAction<float> SetHue { get; }
        public InputAction<float> SetSaturation { get; }
        public InputAction<float> SetValue { get; }
        public InputAction<MvxColor> SelectColor { get; }

        public SelectColorViewModel(IMvxNavigationService navigationService, IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.navigationService = navigationService;
            this.rxActionFactory = rxActionFactory;

            // Public properties
            Hue = hue.AsObservable();
            Saturation = saturation.AsObservable();
            Value = value.AsObservable();

            Save = rxActionFactory.FromAsync(save);
            Close = rxActionFactory.FromAsync(close);
            SetHue = rxActionFactory.FromAction<float>(hue.OnNext);
            SetSaturation = rxActionFactory.FromAction<float>(saturation.OnNext);
            SetValue = rxActionFactory.FromAction<float>(value.OnNext);
            SelectColor = rxActionFactory.FromAction<MvxColor>(selectColor);

            customColor = Observable
                .CombineLatest(hue, saturation, value, Color.FromHSV)
                .Do(selectedColor.OnNext);

            var availableColors = Observable.Return(Color.DefaultProjectColors)
                .CombineLatest(customColor, combineAllColors);

            SelectableColors = availableColors
                .CombineLatest(selectedColor, updateSelectableColors);
        }

        public override void Prepare(ColorParameters parameter)
        {
            defaultColor = parameter.Color;
            AllowCustomColors = parameter.AllowCustomColors;

            var noColorsSelected = Color.DefaultProjectColors.None(color => color == defaultColor);

            if (noColorsSelected)
            {
                if (AllowCustomColors)
                {
                    var colorComponents = defaultColor.GetHSV();
                    hue.OnNext(colorComponents.hue);
                    saturation.OnNext(colorComponents.saturation);
                    value.OnNext(colorComponents.value);
                }
                else
                {
                    selectedColor.OnNext(Color.DefaultProjectColors.First());
                }
            }
            else
            {
                selectedColor.OnNext(defaultColor);
            }
        }

        private IEnumerable<MvxColor> combineAllColors(MvxColor[] defaultColors, MvxColor custom)
        {
            if (AllowCustomColors)
            {
                return defaultColors.Concat(new[] { custom });
            }

            return defaultColors;
        }

        private void selectColor(MvxColor color)
        {
            selectedColor.OnNext(color);

            if (!AllowCustomColors)
                save();
        }

        private IEnumerable<SelectableColorViewModel> updateSelectableColors(IEnumerable<MvxColor> availableColors, MvxColor selectedColor)
            => availableColors.Select(color => new SelectableColorViewModel(color, color == selectedColor));

        private Task close()
            => navigationService.Close(this, defaultColor);

        private Task save()
            => navigationService.Close(this, selectedColor.Value);
    }
}
