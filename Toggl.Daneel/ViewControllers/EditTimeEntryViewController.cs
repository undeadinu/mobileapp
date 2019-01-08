using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Commands;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.Plugin.Visibility;
using MvvmCross.UI;
using MvvmCross.WeakSubscription;
using Toggl.Daneel.Combiners;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Combiners;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Onboarding.EditView;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public partial class EditTimeEntryViewController : MvxViewController<EditTimeEntryViewModel>, IDismissableViewController
    {
        private const float nonScrollableContentHeight = 116f;

        private IDisposable hasProjectDisposable;
        private IDisposable projectOnboardingDisposable;
        private IDisposable contentSizeChangedDisposable;

        private ISubject<bool> hasProjectSubject;

        private const string boundsKey = "bounds";

        public EditTimeEntryViewController() : base(nameof(EditTimeEntryViewController), null)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            contentSizeChangedDisposable?.Dispose();
            contentSizeChangedDisposable = null;

            hasProjectDisposable?.Dispose();
            hasProjectDisposable = null;

            projectOnboardingDisposable?.Dispose();
            projectOnboardingDisposable = null;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TitleLabel.Text = Resources.Edit;
            BillableLabel.Text = Resources.Billable;
            StartDateDescriptionLabel.Text = Resources.Startdate;
            DurationDescriptionLabel.Text = Resources.Duration;
            StartDescriptionLabel.Text = Resources.Start;
            EndDescriptionLabel.Text = Resources.End;
            ErrorMessageTitleLabel.Text = Resources.Oops;
            AddProjectTaskLabel.Text = Resources.AddProjectTask;
            CategorizeWithProjectsLabel.Text = Resources.CategorizeYourTimeWithProjects;
            AddTagsLabel.Text = Resources.AddTags;
            DeleteButton.SetTitle(Resources.Delete, UIControlState.Normal);
            ConfirmButton.SetTitle(Resources.ConfirmChanges, UIControlState.Normal);

            prepareViews();
            prepareOnboarding();

            contentSizeChangedDisposable = ScrollViewContent.AddObserver(boundsKey, NSKeyValueObservingOptions.New, onContentSizeChanged);

            var durationCombiner = new DurationValueCombiner();
            var dateCombiner = new DateTimeOffsetDateFormatValueCombiner(TimeZoneInfo.Local);
            var timeCombiner = new DateTimeOffsetTimeFormatValueCombiner(TimeZoneInfo.Local);
            var visibilityConverter = new MvxVisibilityValueConverter();
            var invertedBoolConverter = new BoolToConstantValueConverter<bool>(false, true);
            var inverterVisibilityConverter = new MvxInvertedVisibilityValueConverter();
            var projectTaskClientCombiner = new ProjectTaskClientValueCombiner(
                ProjectTaskClientLabel.Font.CapHeight,
                Color.EditTimeEntry.ClientText.ToNativeColor(),
                false
            );
            var stopRunningTimeEntryAndSelectStopTimeForStoppedConverter = new BoolToConstantValueConverter<IMvxCommand>(
                ViewModel.StopCommand, ViewModel.SelectStopTimeCommand);

            var isInaccessibleTextColorConverter = new BoolToConstantValueConverter<UIColor>(
                Color.Common.Disabled.ToNativeColor(),
                Color.Common.TextColor.ToNativeColor()
            );

            var showTagsCombiner = new ShowTagsValueCombiner();

            var bindingSet = this.CreateBindingSet<EditTimeEntryViewController, EditTimeEntryViewModel>();

            //Error message view
            bindingSet.Bind(ErrorMessageLabel)
                      .For(v => v.Text)
                      .To(vm => vm.SyncErrorMessage);

            bindingSet.Bind(ErrorView)
                      .For(v => v.BindTap())
                      .To(vm => vm.DismissSyncErrorMessageCommand);

            bindingSet.Bind(ErrorView)
                      .For(v => v.BindVisible())
                      .To(vm => vm.SyncErrorMessageVisible)
                      .WithConversion(inverterVisibilityConverter);

            //Text
            bindingSet.Bind(RemainingCharacterCount)
                      .To(vm => vm.DescriptionRemainingLength);

            bindingSet.Bind(RemainingCharacterCount)
                      .For(v => v.BindVisible())
                      .To(vm => vm.DescriptionLimitExceeded);

            bindingSet.Bind(DescriptionTextView)
                      .For(v => v.BindText())
                      .To(vm => vm.Description);

            bindingSet.Bind(DescriptionTextView)
                      .For(v => v.BindDidBecomeFirstResponder())
                      .To(vm => vm.StartEditingDescriptionCommand);

            bindingSet.Bind(DurationLabel)
                      .ByCombining(durationCombiner,
                          vm => vm.Duration,
                          vm => vm.DurationFormat);

            bindingSet.Bind(ProjectTaskClientLabel)
                      .For(v => v.AttributedText)
                      .ByCombining(projectTaskClientCombiner,
                          v => v.Project,
                          v => v.Task,
                          v => v.Client,
                          v => v.ProjectColor);

            bindingSet.Bind(StartDateLabel)
                      .ByCombining(dateCombiner,
                          vm => vm.StartTime,
                          vm => vm.DateFormat);

            bindingSet.Bind(StartTimeLabel)
                      .ByCombining(timeCombiner,
                          vm => vm.StartTime,
                          vm => vm.TimeFormat);

            bindingSet.Bind(EndTimeLabel)
                      .ByCombining(timeCombiner,
                          vm => vm.StopTime,
                          vm => vm.TimeFormat);

            //Commands
            bindingSet.Bind(CloseButton).To(vm => vm.CloseCommand);
            bindingSet.Bind(DeleteButton).To(vm => vm.DeleteCommand);
            bindingSet.Bind(ConfirmButton).To(vm => vm.SaveCommand);

            bindingSet.Bind(DurationView)
                      .For(v => v.BindTap())
                      .To(vm => vm.SelectDurationCommand);

            bindingSet.Bind(StartTimeView)
                      .For(v => v.BindTap())
                      .To(vm => vm.SelectStartTimeCommand);

            bindingSet.Bind(StopButton)
                      .To(vm => vm.StopCommand);

            bindingSet.Bind(EndTimeView)
                      .For(v => v.BindTap())
                      .To(vm => vm.IsTimeEntryRunning)
                      .WithConversion(stopRunningTimeEntryAndSelectStopTimeForStoppedConverter);

            bindingSet.Bind(ProjectTaskClientLabel)
                      .For(v => v.BindTap())
                      .To(vm => vm.SelectProjectCommand);

            bindingSet.Bind(AddProjectAndTaskView)
                      .For(v => v.BindTap())
                      .To(vm => vm.SelectProjectCommand);

            bindingSet.Bind(StartDateView)
                      .For(v => v.BindTap())
                      .To(vm => vm.SelectStartDateCommand);

            bindingSet.Bind(TagsTextView)
                      .For(v => v.BindTap())
                      .To(vm => vm.SelectTagsCommand);

            bindingSet.Bind(AddTagsView)
                      .For(v => v.BindTap())
                      .To(vm => vm.SelectTagsCommand);

            bindingSet.Bind(BillableSwitch)
                      .For(v => v.BindValueChanged())
                      .To(vm => vm.ToggleBillableCommand);

            //End time and the stop button visibility
            bindingSet.Bind(StopButton)
                      .For(v => v.BindVisible())
                      .To(vm => vm.IsTimeEntryRunning)
                      .WithConversion(inverterVisibilityConverter);

            bindingSet.Bind(EndTimeLabel)
                      .For(v => v.BindVisible())
                      .To(vm => vm.IsTimeEntryRunning)
                      .WithConversion(visibilityConverter);

            //Project visibility
            bindingSet.Bind(AddProjectAndTaskView)
                      .For(v => v.BindVisible())
                      .To(vm => vm.Project)
                      .WithConversion(visibilityConverter);

            bindingSet.Bind(ProjectTaskClientLabel)
                      .For(v => v.BindVisible())
                      .To(vm => vm.Project)
                      .WithConversion(inverterVisibilityConverter);

            //Tags visibility
            bindingSet.Bind(AddTagsView)
                      .For(v => v.BindVisible())
                      .To(vm => vm.HasTags)
                      .WithConversion(visibilityConverter);

            bindingSet.Bind(TagsTextView)
                      .For(v => v.BindVisible())
                      .To(vm => vm.HasTags)
                      .WithConversion(inverterVisibilityConverter);

            //Confirm button enabled
            bindingSet.Bind(ConfirmButton)
                      .For(v => v.Enabled)
                      .To(vm => vm.DescriptionLimitExceeded)
                      .WithConversion(invertedBoolConverter);

            bindingSet.Bind(ConfirmButton)
                      .For(v => v.Alpha)
                      .To(vm => vm.DescriptionLimitExceeded)
                      .WithConversion(new BoolToConstantValueConverter<nfloat>(0.5f, 1));

            bindingSet.Bind(BillableView)
                      .For(v => v.BindVisibility())
                      .To(vm => vm.IsBillableAvailable)
                      .WithConversion(visibilityConverter);

            //Regarding inaccessible entries
            getLabelsToChangeColorWhenEditingInaccessibleEntry().ForEach(createTextColorBindingForInaccessibleEntries);

            bindingSet.Bind(DescriptionTextView)
                      .For(v => v.TextColor)
                      .To(vm => vm.IsInaccessible)
                      .WithConversion(isInaccessibleTextColorConverter);

            bindingSet.Bind(DescriptionTextView)
                      .For(v => v.UserInteractionEnabled)
                      .To(vm => vm.IsInaccessible)
                      .WithConversion(invertedBoolConverter);

            bindingSet.Bind(BillableSwitch)
                      .For(v => v.Enabled)
                      .To(vm => vm.IsInaccessible)
                      .WithConversion(invertedBoolConverter);

            bindingSet.Bind(TagsContainerView)
                      .For(v => v.Hidden)
                      .ByCombining(showTagsCombiner,
                                   vm => vm.IsInaccessible,
                                   vm => vm.HasTags)
                      .WithConversion(invertedBoolConverter);

            bindingSet.Bind(TagsSeparator)
                      .For(v => v.Hidden)
                      .ByCombining(showTagsCombiner,
                                   vm => vm.IsInaccessible,
                                   vm => vm.HasTags)
                      .WithConversion(invertedBoolConverter);

            bindingSet.Apply();

            void createTextColorBindingForInaccessibleEntries(UILabel label)
            {
                bindingSet.Bind(label)
                          .For(v => v.TextColor)
                          .To(vm => vm.IsInaccessible)
                          .WithConversion(isInaccessibleTextColorConverter);
            }
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            //This binding needs to be created, when TagsTextView has it's
            //proper size. In ViewDidLoad() TagsTextView width isn't initialized
            //yet, which results in displaying less tags than possible
            this.CreateBinding(TagsTextView)
                .For(v => v.BindTags())
                .To<EditTimeEntryViewModel>(vm => vm.Tags)
                .Apply();
        }

        public override void ViewWillLayoutSubviews()
        {
            adjustHeight();
        }

        public async Task<bool> Dismiss()
        {
            return await ViewModel.CloseWithConfirmation();
        }

        IEnumerable<UILabel> getLabelsToChangeColorWhenEditingInaccessibleEntry()
        {
            yield return StartTimeLabel;
            yield return StartDateLabel;
            yield return EndTimeLabel;
            yield return DurationLabel;
        }

        private void prepareViews()
        {
            DurationLabel.Font = DurationLabel.Font.GetMonospacedDigitFont();
            PreferredContentSize = View.Frame.Size;
            prepareDescriptionField();
            centerTextVertically(TagsTextView);
            TagsTextView.TextContainer.LineFragmentPadding = 0;
            BillableSwitch.SetState(ViewModel.Billable, false);

            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                var bottomSafeAreaInset = UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Bottom;
                if (bottomSafeAreaInset >= DeleteButtonBottomConstraint.Constant)
                    DeleteButtonBottomConstraint.Constant
                        = ConfirmButtonBottomConstraint.Constant
                        = 0;
            }
        }

        private void prepareDescriptionField()
        {
            DescriptionTextView.TintColor = Color.StartTimeEntry.Cursor.ToNativeColor();
            DescriptionTextView.PlaceholderText = Resources.AddDescription;
        }

        private void centerTextVertically(UITextView textView)
        {
            var topOffset = (textView.Bounds.Height - textView.ContentSize.Height) / 2;
            textView.ContentInset = new UIEdgeInsets(topOffset, 0, 0, 0);
        }

        private void prepareOnboarding()
        {
            var storage = ViewModel.OnboardingStorage;

            hasProjectSubject = new BehaviorSubject<bool>(!String.IsNullOrEmpty(ViewModel.Project));
            hasProjectDisposable = ViewModel.WeakSubscribe(() => ViewModel.Project, onProjectChanged);

            projectOnboardingDisposable = new CategorizeTimeUsingProjectsOnboardingStep(storage, hasProjectSubject.AsObservable())
                .ManageDismissableTooltip(CategorizeWithProjectsBubbleView, storage);
        }

        private void onProjectChanged(object sender, PropertyChangedEventArgs args)
        {
            hasProjectSubject.OnNext(!String.IsNullOrEmpty(ViewModel.Project));
        }

        private void onContentSizeChanged(NSObservedChange change)
        {
            adjustHeight();
        }

        private void adjustHeight()
        {
            var height = nonScrollableContentHeight + ScrollViewContent.Bounds.Height;
            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                height += UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Bottom;
            }

            var newSize = new CGSize(0, height);
            if (newSize != PreferredContentSize)
            {
                PreferredContentSize = newSize;
                PresentationController.ContainerViewWillLayoutSubviews();
            }

            ScrollView.ScrollEnabled = ScrollViewContent.Bounds.Height > ScrollView.Bounds.Height;
        }
    }
}
