using System;
using System.Reactive.Linq;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;
using Math = System.Math;

namespace Toggl.Daneel.ViewControllers
{
    [MvxChildPresentation]
    public partial class SettingsViewController : ReactiveViewController<SettingsViewModel>
    {
        private const int verticalSpacing = 24;

        public SettingsViewController()
            : base(nameof(SettingsViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            YourProfileCellLabel.Text = Resources.YourProfile;
            WorkspaceCellLabel.Text = Resources.Workspace;
            FormatSettingsHeaderLabel.Text = Resources.FormatSettings;
            DateFormatCellLabel.Text = Resources.DateFormat;
            Use24HourClockCellLabel.Text = Resources.Use24HourClock;
            DurationFormatCellLabel.Text = Resources.DurationFormat;
            FirstDayOfTheWeekCellLabel.Text = Resources.FirstDayOfTheWeek;
            ManualModeCellLabel.Text = Resources.ManualMode;
            ManualModeDescriptionLabel.Text = Resources.ManualModeDescription;
            CalendarSettingsCellLabel.Text = Resources.CalendarSettingsTitle;
            SmartAlertCellLabel.Text = Resources.SmartAlerts;
            SubmitFeedbackCellLabel.Text = Resources.SubmitFeedback;
            AboutCellLabel.Text = Resources.About;
            HelpCellLabel.Text = Resources.Help;
            LoggingOutLabel.Text = Resources.LoggingOutSecurely;
            SyncingLabel.Text = Resources.Syncing;
            SyncedLabel.Text = Resources.SyncCompleted;
            FeedbackToastTitleLabel.Text = Resources.DoneWithExclamationMark.ToUpper();
            FeedbackToastTextLabel.Text = Resources.ThankYouForTheFeedback;
            LogoutButton.SetTitle(Resources.SignOutOfToggl, UIControlState.Normal);

            prepareViews();

            Title = ViewModel.Title;
            VersionLabel.Text = ViewModel.Version;

            LoggingOutView.Hidden = true;
            SendFeedbackSuccessView.Hidden = true;

            ViewModel.Email
                .Subscribe(EmailLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.IsSynced
                .Subscribe(SyncedView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.WorkspaceName
                .Subscribe(WorkspaceLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.DurationFormat
                .Subscribe(DurationFormatLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.IsRunningSync
                .Subscribe(SyncingView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.DateFormat
                .Subscribe(DateFormatLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.BeginningOfWeek
                .Subscribe(BeginningOfWeekLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.IsFeedbackSuccessViewShowing
                .Subscribe(SendFeedbackSuccessView.Rx().AnimatedIsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.LoggingOut
                .Subscribe(_ =>
                {
                    LoggingOutView.Hidden = false;
                    SyncingView.Hidden = true;
                    SyncedView.Hidden = true;
                })
                .DisposedBy(DisposeBag);

            HelpView.Rx()
                .BindAction(ViewModel.OpenHelpView)
                .DisposedBy(DisposeBag);

            LogoutButton.Rx()
                .BindAction(ViewModel.TryLogout)
                .DisposedBy(DisposeBag);

            AboutView.Rx()
                .BindAction(ViewModel.OpenAboutView)
                .DisposedBy(DisposeBag);

            FeedbackView.Rx()
                .BindAction(ViewModel.SubmitFeedback)
                .DisposedBy(DisposeBag);

            DateFormatView.Rx()
                .BindAction(ViewModel.SelectDateFormat)
                .DisposedBy(DisposeBag);

            WorkspaceView.Rx()
                .BindAction(ViewModel.PickDefaultWorkspace)
                .DisposedBy(DisposeBag);

            DurationFormatView.Rx()
                .BindAction(ViewModel.SelectDurationFormat)
                .DisposedBy(DisposeBag);

            ManualModeSwitch.Rx().Changed()
                .Subscribe(ViewModel.ToggleManualMode)
                .DisposedBy(DisposeBag);

            BeginningOfWeekView.Rx()
                .BindAction(ViewModel.SelectBeginningOfWeek)
                .DisposedBy(DisposeBag);

            CalendarSettingsView.Rx()
                .BindAction(ViewModel.OpenCalendarSettings)
                .DisposedBy(DisposeBag);

            SendFeedbackSuccessView.Rx().Tap()
                .Subscribe(ViewModel.CloseFeedbackSuccessView)
                .DisposedBy(DisposeBag);

            NotificationSettingsView.Rx()
                .BindAction(ViewModel.OpenNotificationSettings)
                .DisposedBy(DisposeBag);

            TwentyFourHourClockSwitch.Rx().Changed()
                .Subscribe(ViewModel.ToggleTwentyFourHourSettings.Inputs)
                .DisposedBy(DisposeBag);

            UIApplication.Notifications
                .ObserveWillEnterForeground((sender, e) => startAnimations())
                .DisposedBy(DisposeBag);

            if (!ViewModel.CalendarSettingsEnabled)
                hideCalendarSettingsSection();

            ViewModel.IsManualModeEnabled
                .FirstAsync()
                .Subscribe(isEnabled => ManualModeSwitch.SetState(isEnabled, false))
                .DisposedBy(DisposeBag);

            ViewModel.UseTwentyFourHourFormat
                .FirstAsync()
                .Subscribe(useTwentyFourHourFormat => TwentyFourHourClockSwitch.SetState(useTwentyFourHourFormat, false))
                .DisposedBy(DisposeBag);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            startAnimations();
        }

        public override void ViewWillLayoutSubviews()
        {
            base.ViewWillLayoutSubviews();
            tryAlignLogoutButtonWithBottomEdge();
        }

        private void prepareViews()
        {
            // Syncing indicator colors
            setIndicatorSyncColor(SyncedIcon);
            setIndicatorSyncColor(SyncingIndicator);
            setIndicatorSyncColor(LoggingOutIndicator);
        }

        private void hideCalendarSettingsSection()
        {
            CalendarSettingsSection.Hidden = true;
            CalendarSectionTopConstraint.Constant = 0;
            CalendarSettingsSection.HeightAnchor.ConstraintEqualTo(0).Active = true;
        }

        private void setIndicatorSyncColor(UIImageView imageView)
        {
            imageView.Image = imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            imageView.TintColor = Color.Settings.SyncStatusText.ToNativeColor();
        }

        private void startAnimations()
        {
            SyncingActivityIndicatorView.StartSpinning();
            LoggingOutActivityIndicatorView.StartSpinning();
        }

        private void tryAlignLogoutButtonWithBottomEdge()
        {
            var contentHeight = LogoutContainerView.Frame.Top - LogoutVerticalOffsetConstraint.Constant + LogoutContainerView.Frame.Height;
            var bottomOffset = verticalSpacing;
            var idealDistance = ScrollView.Frame.Height - contentHeight - bottomOffset;
            var distance = Math.Max(idealDistance, verticalSpacing);
            LogoutVerticalOffsetConstraint.Constant = (nfloat)distance;
        }
    }
}
