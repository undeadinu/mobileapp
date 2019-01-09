using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;
using static Toggl.Multivac.Extensions.CommonFunctions;

namespace Toggl.Daneel.ViewControllers.Settings
{
    [ModalCardPresentation]
    public sealed partial class SendFeedbackViewController : KeyboardAwareViewController<SendFeedbackViewModel>, IDismissableViewController
    {

        public SendFeedbackViewController()
            : base(nameof(SendFeedbackViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TitleLabel.Text = Resources.ContactUs;
            FeedbackPlaceholderTextView.Text = Resources.FeedbackFieldPlaceholder;
            ErrorTitleLabel.Text = Resources.Oops.ToUpper();
            ErrorMessageLabel.Text = Resources.SomethingWentWrongTryAgain;
            SendButton.SetTitle(Resources.Send, UIControlState.Normal);

            prepareViews();
            prepareIndicatorView();

            CloseButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            FeedbackTextView.Rx().Text()
                .Subscribe(ViewModel.FeedbackText)
                .DisposedBy(DisposeBag);

            ErrorView.Rx()
                .BindAction(ViewModel.DismissError)
                .DisposedBy(DisposeBag);

            SendButton.Rx()
                .BindAction(ViewModel.Send)
                .DisposedBy(DisposeBag);
            SendButton.TouchUpInside += (sender, args) => { FeedbackTextView.ResignFirstResponder(); };

            ViewModel.IsFeedbackEmpty
                .Subscribe(FeedbackPlaceholderTextView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.Error
                .Select(NotNull)
                .Subscribe(ErrorView.Rx().AnimatedIsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.SendEnabled
                .Subscribe(SendButton.Rx().Enabled())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Invert()
                .Subscribe(SendButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Invert()
                .Subscribe(CloseButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Subscribe(IndicatorView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Subscribe(UIApplication.SharedApplication.Rx().NetworkActivityIndicatorVisible())
                .DisposedBy(DisposeBag);
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            UIEdgeInsets contentInsets = new UIEdgeInsets(0, 0, e.FrameEnd.Height, 0);
            FeedbackTextView.ContentInset = contentInsets;
            FeedbackTextView.ScrollIndicatorInsets = contentInsets;
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            FeedbackTextView.ContentInset = UIEdgeInsets.Zero;
            FeedbackTextView.ScrollIndicatorInsets = UIEdgeInsets.Zero;
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            IndicatorView.StartSpinning();
        }

        public async Task<bool> Dismiss()
        {
            await ViewModel.Close.Execute();
            return true;
        }

        private void prepareViews()
        {
            ErrorView.Hidden = true;
            FeedbackTextView.TintColor = Color.Feedback.Cursor.ToNativeColor();
            FeedbackPlaceholderTextView.TintColor = Color.Feedback.Cursor.ToNativeColor();
        }

        private void prepareIndicatorView()
        {
            IndicatorView.IndicatorColor = Color.Feedback.ActivityIndicator.ToNativeColor();
            IndicatorView.Hidden = true;
        }
    }
}

