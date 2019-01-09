using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using MvvmCross.Binding;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Commands;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.Plugin.Visibility;
using Toggl.Daneel.Autocomplete;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Combiners;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Onboarding.CreationView;
using Toggl.Foundation.MvvmCross.Onboarding.StartTimeEntryView;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class StartTimeEntryViewController : KeyboardAwareViewController<StartTimeEntryViewModel>, IDismissableViewController
    {
        private bool isUpdatingDescriptionField;

        private UIImage greyCheckmarkButtonImage;
        private UIImage greenCheckmarkButtonImage;

        private IDisposable descriptionDisposable;
        private IDisposable addProjectOrTagOnboardingDisposable;
        private IDisposable disabledConfirmationButtonOnboardingDisposable;

        private ISubject<bool> isDescriptionEmptySubject = new BehaviorSubject<bool>(true);

        private IUITextInputDelegate emptyInputDelegate = new EmptyInputDelegate();

        public StartTimeEntryViewController()
            : base(nameof(StartTimeEntryViewController))
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            descriptionDisposable?.Dispose();
            descriptionDisposable = null;

            addProjectOrTagOnboardingDisposable?.Dispose();
            addProjectOrTagOnboardingDisposable = null;

            disabledConfirmationButtonOnboardingDisposable?.Dispose();
            disabledConfirmationButtonOnboardingDisposable = null;

            TimeInput.LostFocus -= onTimeInputLostFocus;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            AddProjectBubbleLabel.Text = Resources.AddProjectBubbleText;

            prepareViews();
            prepareOnboarding();

            var source = new StartTimeEntryTableViewSource(SuggestionsTableView);
            SuggestionsTableView.Source = source;
            source.ToggleTasksCommand = new MvxCommand<ProjectSuggestion>(toggleTaskSuggestions);

            var invertedVisibilityConverter = new MvxInvertedVisibilityValueConverter();
            var invertedBoolConverter = new BoolToConstantValueConverter<bool>(false, true);
            var buttonColorConverter = new BoolToConstantValueConverter<UIColor>(
                Color.StartTimeEntry.ActiveButton.ToNativeColor(),
                Color.StartTimeEntry.InactiveButton.ToNativeColor()
            );
            var durationCombiner = new DurationValueCombiner();

            var bindingSet = this.CreateBindingSet<StartTimeEntryViewController, StartTimeEntryViewModel>();

            //TableView
            bindingSet.Bind(source)
                      .For(v => v.ObservableCollection)
                      .To(vm => vm.Suggestions);

            bindingSet.Bind(source)
                      .For(v => v.UseGrouping)
                      .To(vm => vm.UseGrouping);

            bindingSet.Bind(source)
                      .For(v => v.SelectSuggestionCommand)
                      .To(vm => vm.SelectSuggestionCommand);

            bindingSet.Bind(source)
                      .For(v => v.CreateCommand)
                      .To(vm => vm.CreateCommand);

            bindingSet.Bind(source)
                      .For(v => v.IsSuggestingProjects)
                      .To(vm => vm.IsSuggestingProjects);

            bindingSet.Bind(source)
                      .For(v => v.Text)
                      .To(vm => vm.CurrentQuery);

            bindingSet.Bind(source)
                      .For(v => v.SuggestCreation)
                      .To(vm => vm.SuggestCreation);

            bindingSet.Bind(source)
                      .For(v => v.ShouldShowNoTagsInfoMessage)
                      .To(vm => vm.ShouldShowNoTagsInfoMessage);

            bindingSet.Bind(source)
                      .For(v => v.ShouldShowNoProjectsInfoMessage)
                      .To(vm => vm.ShouldShowNoProjectsInfoMessage);

            //Text
            bindingSet.Bind(TimeInput)
                      .For(v => v.Duration)
                      .To(vm => vm.DisplayedTime)
                      .Mode(MvxBindingMode.OneWayToSource);

            bindingSet.Bind(TimeLabel)
                      .For(v => v.Text)
                      .ByCombining(durationCombiner,
                          vm => vm.DisplayedTime,
                          vm => vm.DisplayedTimeFormat);

            bindingSet.Bind(Placeholder)
                      .To(vm => vm.PlaceholderText);

            bindingSet.Bind(DescriptionRemainingLengthLabel)
                      .To(vm => vm.DescriptionRemainingBytes);

            //Buttons
            bindingSet.Bind(TagsButton)
                      .For(v => v.TintColor)
                      .To(vm => vm.IsSuggestingTags)
                      .WithConversion(buttonColorConverter);

            bindingSet.Bind(BillableButton)
                      .For(v => v.TintColor)
                      .To(vm => vm.IsBillable)
                      .WithConversion(buttonColorConverter);

            bindingSet.Bind(ProjectsButton)
                      .For(v => v.TintColor)
                      .To(vm => vm.IsSuggestingProjects)
                      .WithConversion(buttonColorConverter);

            bindingSet.Bind(DoneButton)
                      .For(v => v.Enabled)
                      .To(vm => vm.DescriptionLengthExceeded)
                      .WithConversion(invertedBoolConverter);

            //Visibility
            bindingSet.Bind(BillableButtonWidthConstraint)
                      .For(v => v.Constant)
                      .To(vm => vm.IsBillableAvailable)
                      .WithConversion(new BoolToConstantValueConverter<nfloat>(42, 0));

            bindingSet.Bind(DescriptionRemainingLengthLabel)
                      .For(v => v.BindVisible())
                      .To(vm => vm.DescriptionLengthExceeded)
                      .WithConversion(invertedVisibilityConverter);

            //Commands
            bindingSet.Bind(DoneButton).To(vm => vm.DoneCommand);
            bindingSet.Bind(CloseButton).To(vm => vm.BackCommand);
            bindingSet.Bind(BillableButton).To(vm => vm.ToggleBillableCommand);
            bindingSet.Bind(StartDateButton).To(vm => vm.SetStartDateCommand);
            bindingSet.Bind(DateTimeButton).To(vm => vm.ChangeTimeCommand);
            bindingSet.Bind(TagsButton).To(vm => vm.ToggleTagSuggestionsCommand);
            bindingSet.Bind(ProjectsButton).To(vm => vm.ToggleProjectSuggestionsCommand);

            bindingSet.Apply();

            // Reactive
            ViewModel.TextFieldInfoObservable
                .Subscribe(onTextFieldInfo)
                .DisposedBy(DisposeBag);

            DescriptionTextView.Rx().AttributedText()
                .Select(attributedString => attributedString.Length == 0)
                .Subscribe(isDescriptionEmptySubject)
                .DisposedBy(DisposeBag);

            DescriptionTextView.Rx().AttributedText()
                .CombineLatest(DescriptionTextView.Rx().CursorPosition(), (text, _) => text)
                .Where(_ => !isUpdatingDescriptionField)
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .Do(updatePlaceholder)
                .Select(text => text.AsImmutableSpans((int)DescriptionTextView.SelectedRange.Location))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(async info => await ViewModel.OnTextFieldInfoFromView(info))
                .DisposedBy(DisposeBag);

            source.TableRenderCallback = () =>
            {
                ViewModel.StopSuggestionsRenderingStopwatch();
            };
        }

        public async Task<bool> Dismiss()
        {
            return await ViewModel.Close();
        }

        private void onTextFieldInfo(TextFieldInfo textFieldInfo)
        {
            isUpdatingDescriptionField = true;
            var (attributedText, cursorPosition) = textFieldInfo.AsAttributedTextAndCursorPosition();

            DescriptionTextView.InputDelegate = emptyInputDelegate; //This line is needed for when the user selects from suggestion and the iOS autocorrect is ready to add text at the same time. Without this line both will happen.
            DescriptionTextView.AttributedText = attributedText;

            var positionToSet = DescriptionTextView.GetPosition(DescriptionTextView.BeginningOfDocument, cursorPosition);
            DescriptionTextView.SelectedTextRange = DescriptionTextView.GetTextRange(positionToSet, positionToSet);
            updatePlaceholder();
            isUpdatingDescriptionField = false;
        }

        private void switchTimeLabelAndInput()
        {
            TimeLabel.Hidden = !TimeLabel.Hidden;
            TimeInput.Hidden = !TimeInput.Hidden;

            TimeLabelTrailingConstraint.Active = !TimeLabel.Hidden;
            TimeInputTrailingConstraint.Active = !TimeInput.Hidden;
        }

        private void updatePlaceholder()
        {
            Placeholder.UpdateVisibility(DescriptionTextView);
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            BottomDistanceConstraint.Constant = e.FrameEnd.Height;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            BottomDistanceConstraint.Constant = 0;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            base.TouchesBegan(touches, evt);

            if (TimeInput.IsEditing)
            {
                TimeInput.EndEditing(true);
                DescriptionTextView.BecomeFirstResponder();
            }
        }

        private void prepareViews()
        {
            //This is needed for the ImageView.TintColor bindings to work
            foreach (var button in getButtons())
            {
                button.SetImage(
                    button.ImageForState(UIControlState.Normal)
                          .ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate),
                    UIControlState.Normal
                );
                button.TintColor = Color.StartTimeEntry.InactiveButton.ToNativeColor();
            }

            TimeInput.TintColor = Color.StartTimeEntry.Cursor.ToNativeColor();

            DescriptionTextView.TintColor = Color.StartTimeEntry.Cursor.ToNativeColor();
            DescriptionTextView.BecomeFirstResponder();

            Placeholder.ConfigureWith(DescriptionTextView);
            Placeholder.Text = Resources.StartTimeEntryPlaceholder;

            prepareTimeViews();
        }

        private void prepareTimeViews()
        {
            var tapRecognizer = new UITapGestureRecognizer(() =>
            {
                if (!TimeLabel.Hidden)
                    ViewModel.DurationTapped.Execute();

                switchTimeLabelAndInput();

                if (!TimeInput.Hidden)
                {
                    TimeInput.FormattedDuration = TimeLabel.Text;
                    TimeInput.BecomeFirstResponder();
                }
            });

            TimeLabel.UserInteractionEnabled = true;
            TimeLabel.AddGestureRecognizer(tapRecognizer);

            TimeInput.LostFocus += onTimeInputLostFocus;
        }

        private void onTimeInputLostFocus(object sender, EventArgs e)
            => switchTimeLabelAndInput();

        private IEnumerable<UIButton> getButtons()
        {
            yield return TagsButton;
            yield return ProjectsButton;
            yield return BillableButton;
            yield return StartDateButton;
            yield return DateTimeButton;
            yield return DateTimeButton;
        }

        private void toggleTaskSuggestions(ProjectSuggestion parameter)
        {
            var offset = SuggestionsTableView.ContentOffset;
            var frameHeight = SuggestionsTableView.Frame.Height;

            ViewModel.ToggleTaskSuggestionsCommand.Execute(parameter);

            SuggestionsTableView.CorrectOffset(offset, frameHeight);
        }

        private void prepareOnboarding()
        {
            prepareAddProjectOnboardingStep();
            prepareDisableConfirmationButtonOnboardingStep();
        }

        private void prepareAddProjectOnboardingStep()
        {
            var onboardingStorage = ViewModel.OnboardingStorage;
            var addProjectOrtagOnboardingStep = new AddProjectOrTagOnboardingStep(
                onboardingStorage,
                ViewModel.DataSource
            );

            addProjectOrTagOnboardingDisposable = addProjectOrtagOnboardingStep
                .ManageDismissableTooltip(AddProjectOnboardingBubble, onboardingStorage);
        }

        private void prepareDisableConfirmationButtonOnboardingStep()
        {
            greyCheckmarkButtonImage = UIImage.FromBundle("icCheckGrey");
            greenCheckmarkButtonImage = UIImage.FromBundle("doneGreen");

            var disabledConfirmationButtonOnboardingStep
                = new DisabledConfirmationButtonOnboardingStep(
                    ViewModel.OnboardingStorage,
                    isDescriptionEmptySubject.AsObservable());

            disabledConfirmationButtonOnboardingDisposable
                = disabledConfirmationButtonOnboardingStep
                    .ShouldBeVisible
                    .Subscribe(visible => InvokeOnMainThread(() =>
                    {
                        var image = visible ? greyCheckmarkButtonImage : greenCheckmarkButtonImage;
                        DoneButton.SetImage(image, UIControlState.Normal);
                    }));
        }
    }
}

