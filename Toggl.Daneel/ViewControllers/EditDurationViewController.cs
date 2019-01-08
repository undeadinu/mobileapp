using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Converters;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.MvvmCross.Combiners;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;
using static Toggl.Daneel.Extensions.FontExtensions;
using System.Runtime.Remoting.Messaging;
using Toggl.Foundation;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class EditDurationViewController : KeyboardAwareViewController<EditDurationViewModel>, IDismissableViewController
    {
        private const int offsetFromSafeAreaTop = 20;
        private const int additionalVerticalContentSize = 100;
        private const int stackViewSpacing = 26;

        private IDisposable startTimeChangingSubscription;

        private CompositeDisposable disposeBag = new CompositeDisposable();
        private CGRect frameBeforeShowingKeyboard;

        public EditDurationViewController() : base(nameof(EditDurationViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            StartLabel.Text = Resources.Start;
            EndLabel.Text = Resources.End;
            TitleLabel.Text = Resources.StartAndStopTime;
            SetEndButton.SetTitle(Resources.Stop, UIControlState.Normal);
            SaveButton.SetTitle(Resources.Save, UIControlState.Normal);

            startTimeChangingSubscription = ViewModel.StartTimeChanging.Subscribe(startTimeChanging);

            prepareViews();

            var durationCombiner = new DurationValueCombiner();
            var timeCombiner = new DateTimeOffsetTimeFormatValueCombiner(TimeZoneInfo.Local);
            var dateCombiner = new DateTimeOffsetDateFormatValueCombiner(TimeZoneInfo.Local, useLongFormat: false);
            var timeFormatToLocaleConverter = new TimeFormatToLocaleValueConverter();
            var inverseBoolConverter = new BoolToConstantValueConverter<bool>(false, true);
            var editedTimeLabelColorConverter = new BoolToConstantValueConverter<UIColor>(
                Color.EditDuration.EditedTime.ToNativeColor(),
                Color.EditDuration.NotEditedTime.ToNativeColor());

            var bindingSet = this.CreateBindingSet<EditDurationViewController, EditDurationViewModel>();

            //Commands
            bindingSet.Bind(SaveButton).To(vm => vm.SaveCommand);
            bindingSet.Bind(CloseButton).To(vm => vm.CloseCommand);

            //Start and stop date/time
            bindingSet.Bind(StartTimeLabel)
                      .ByCombining(timeCombiner,
                          vm => vm.StartTime,
                          vm => vm.TimeFormat);

            bindingSet.Bind(StartDateLabel)
                      .ByCombining(dateCombiner,
                          vm => vm.StartTime,
                          vm => vm.DateFormat);

            bindingSet.Bind(EndTimeLabel)
                      .ByCombining(timeCombiner,
                          vm => vm.StopTime,
                          vm => vm.TimeFormat);

            bindingSet.Bind(EndDateLabel)
                      .ByCombining(dateCombiner,
                          vm => vm.StopTime,
                          vm => vm.DateFormat);

            //Editing start and end time
            bindingSet.Bind(StartView)
                      .For(v => v.BindTap())
                      .To(vm => vm.EditStartTimeCommand);

            bindingSet.Bind(EndView)
                      .For(v => v.BindTap())
                      .To(vm => vm.EditStopTimeCommand);

            bindingSet.Bind(SetEndButton)
                      .For(v => v.BindVisibility())
                      .To(vm => vm.IsRunning)
                      .WithConversion(inverseBoolConverter);

            bindingSet.Bind(SetEndButton)
                      .To(vm => vm.EditStopTimeCommand);

            //Visibility
            bindingSet.Bind(EndTimeLabel)
                      .For(v => v.BindVisibility())
                      .To(vm => vm.IsRunning);

            bindingSet.Bind(EndDateLabel)
                      .For(v => v.BindVisibility())
                      .To(vm => vm.IsRunning);

            //Stard and end colors
            bindingSet.Bind(StartTimeLabel)
                      .For(v => v.TextColor)
                      .To(vm => vm.IsEditingStartTime)
                      .WithConversion(editedTimeLabelColorConverter);

            bindingSet.Bind(StartDateLabel)
                      .For(v => v.TextColor)
                      .To(vm => vm.IsEditingStartTime)
                      .WithConversion(editedTimeLabelColorConverter);

            bindingSet.Bind(EndTimeLabel)
                      .For(v => v.TextColor)
                      .To(vm => vm.IsEditingStopTime)
                      .WithConversion(editedTimeLabelColorConverter);

            bindingSet.Bind(EndDateLabel)
                      .For(v => v.TextColor)
                      .To(vm => vm.IsEditingStopTime)
                      .WithConversion(editedTimeLabelColorConverter);

            //Date picker
            bindingSet.Bind(DatePickerContainer)
                      .For(v => v.BindAnimatedVisibility())
                      .To(vm => vm.IsEditingTime);

            bindingSet.Bind(DatePicker)
                      .For(v => v.BindDateTimeOffset())
                      .To(vm => vm.EditedTime);

            bindingSet.Bind(DatePicker)
                      .For(v => v.MaximumDate)
                      .To(vm => vm.MaximumDateTime);

            bindingSet.Bind(DatePicker)
                      .For(v => v.MinimumDate)
                      .To(vm => vm.MinimumDateTime);

            bindingSet.Bind(DatePicker)
                      .For(v => v.Locale)
                      .To(vm => vm.TimeFormat)
                      .WithConversion(timeFormatToLocaleConverter);

            //The wheel
            bindingSet.Bind(DurationInput)
                      .For(v => v.UserInteractionEnabled)
                      .To(vm => vm.IsEditingTime)
                      .WithConversion(inverseBoolConverter);

            bindingSet.Bind(DurationInput)
                      .For(v => v.Duration)
                      .To(vm => vm.Duration);

            bindingSet.Bind(DurationInput)
                      .For(v => v.FormattedDuration)
                      .ByCombining(durationCombiner,
                          vm => vm.Duration,
                          vm => vm.DurationFormat);

            bindingSet.Bind(WheelView)
                      .For(v => v.UserInteractionEnabled)
                      .To(vm => vm.IsEditingTime)
                      .WithConversion(inverseBoolConverter);

            bindingSet.Bind(WheelView)
                      .For(v => v.MaximumStartTime)
                      .To(vm => vm.MaximumStartTime);

            bindingSet.Bind(WheelView)
                      .For(v => v.MinimumStartTime)
                      .To(vm => vm.MinimumStartTime);

            bindingSet.Bind(WheelView)
                      .For(v => v.MaximumEndTime)
                      .To(vm => vm.MaximumStopTime);

            bindingSet.Bind(WheelView)
                      .For(v => v.MinimumEndTime)
                      .To(vm => vm.MinimumStopTime);

            bindingSet.Bind(WheelView)
                      .For(v => v.StartTime)
                      .To(vm => vm.StartTime);

            bindingSet.Bind(WheelView)
                      .For(v => v.EndTime)
                      .To(vm => vm.StopTime);

            bindingSet.Bind(WheelView)
                      .For(v => v.IsRunning)
                      .To(vm => vm.IsRunning);

            bindingSet.Apply();

            // Interaction observables for analytics

            var editingStart = Observable.Merge(
                StartView.Rx().Tap().SelectValue(true),
                EndView.Rx().Tap().SelectValue(false)
            );

            var dateComponentChanged = DatePicker.Rx().DateComponent()
                .WithLatestFrom(editingStart,
                    (_, isStart) => isStart ? EditTimeSource.BarrelStartDate : EditTimeSource.BarrelStopDate
                 );

            var timeComponentChanged = DatePicker.Rx().TimeComponent()
                .WithLatestFrom(editingStart,
                    (_, isStart) => isStart ? EditTimeSource.BarrelStartTime : EditTimeSource.BarrelStopTime
                 );

            var durationInputChanged = Observable
                .FromEventPattern(e => DurationInput.DurationChanged += e, e => DurationInput.DurationChanged -= e)
                .SelectValue(EditTimeSource.NumpadDuration);

            Observable.Merge(
                    dateComponentChanged,
                    timeComponentChanged,
                    WheelView.TimeEdited,
                    durationInputChanged
                )
                .Distinct()
                .Subscribe(ViewModel.TimeEditedWithSource)
                .DisposedBy(disposeBag);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            disposeBag?.Dispose();

            startTimeChangingSubscription?.Dispose();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            if (ViewModel.IsDurationInitiallyFocused)
            {
                DurationInput.BecomeFirstResponder();
            }
        }

        public async Task<bool> Dismiss()
        {
            await ViewModel.CloseCommand.ExecuteAsync();
            return true;
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            frameBeforeShowingKeyboard = View.Frame;

            var safeAreaOffset = UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
                  ? Math.Max(UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Top, UIApplication.SharedApplication.StatusBarFrame.Height)
                  : 0;
            var distanceFromTop = Math.Max(safeAreaOffset, View.Frame.Y - e.FrameEnd.Height);

            View.Frame = new CGRect(View.Frame.X, distanceFromTop, View.Frame.Width, View.Frame.Height);
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            View.Frame = frameBeforeShowingKeyboard;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void prepareViews()
        {
            var width = UIScreen.MainScreen.Bounds.Width;
            var height = width + additionalVerticalContentSize;

            PreferredContentSize = new CGSize
            {
                Width = width,
                Height = height
            };

            EndTimeLabel.Font = EndTimeLabel.Font.GetMonospacedDigitFont();
            StartTimeLabel.Font = StartTimeLabel.Font.GetMonospacedDigitFont();

            SetEndButton.TintColor = Color.EditDuration.SetButton.ToNativeColor();

            StackView.Spacing = stackViewSpacing;

            var backgroundTap = new UITapGestureRecognizer(onBackgroundTap);
            View.AddGestureRecognizer(backgroundTap);

            var editTimeTap = new UITapGestureRecognizer(onEditTimeTap);
            StartTimeLabel.AddGestureRecognizer(editTimeTap);
            EndTimeLabel.AddGestureRecognizer(editTimeTap);
        }

        private void onEditTimeTap(UITapGestureRecognizer recognizer)
        {
            if (DurationInput.IsEditing)
                DurationInput.ResignFirstResponder();
        }

        private void onBackgroundTap(UITapGestureRecognizer recognizer)
        {
            if (DurationInput.IsEditing)
                DurationInput.ResignFirstResponder();

            if (ViewModel.IsEditingTime)
                ViewModel.StopEditingTimeCommand.Execute();
        }

        private void startTimeChanging(Unit _)
            => DatePicker.Date = ViewModel.StartTime.Add(ViewModel.Duration).ToNSDate();
    }
}

