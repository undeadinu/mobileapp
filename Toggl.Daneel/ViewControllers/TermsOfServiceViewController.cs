using System;
using CoreGraphics;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;
using static Toggl.Daneel.Extensions.RangeExtensions;

namespace Toggl.Daneel.ViewControllers
{
    [ModalDialogPresentation]
    public sealed partial class TermsOfServiceViewController
        : ReactiveViewController<TermsOfServiceViewModel>
    {
        private const int fontSize = 15;

        private readonly NSRange termsOfServiceTextRange = new NSRange(56, 16);
        private readonly NSRange privacyPolicyRange = new NSRange(77, 14);

        private readonly UIStringAttributes normalTextAttributes = new UIStringAttributes
        {
            Font = UIFont.SystemFontOfSize(fontSize)
        };

        private readonly UIStringAttributes highlitedTextAttributes = new UIStringAttributes
        {
            Font = UIFont.SystemFontOfSize(fontSize),
            ForegroundColor = Color.Signup.HighlightedText.ToNativeColor()
        };

        public TermsOfServiceViewController() : base(nameof(TermsOfServiceViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            PreferredContentSize = new CGSize(View.Frame.Width, View.Frame.Height);
            prepareTextView();

            AcceptButton.Rx()
                .BindAction(ViewModel.Close, _ => true)
                .DisposedBy(DisposeBag);

            CloseButton.Rx()
                .BindAction(ViewModel.Close, _ => false)
                .DisposedBy(DisposeBag);
        }

        private void prepareTextView()
        {
            TextView.TextContainerInset = UIEdgeInsets.Zero;
            TextView.TextContainer.LineFragmentPadding = 0;

            var text = new NSMutableAttributedString(Resources.TermsOfServiceDialogMessage, normalTextAttributes);
            text.AddAttributes(highlitedTextAttributes, termsOfServiceTextRange);
            text.AddAttributes(highlitedTextAttributes, privacyPolicyRange);
            TextView.AttributedText = text;

            TextView.AddGestureRecognizer(new UITapGestureRecognizer(onTextViewTapped));
        }

        private void onTextViewTapped(UITapGestureRecognizer recognizer)
        {
            var layoutManager = TextView.LayoutManager;
            var location = recognizer.LocationInView(TextView);
            location.X -= TextView.TextContainerInset.Left;
            location.Y -= TextView.TextContainerInset.Top;

            nfloat _ = 0;
            var characterIndex = layoutManager.CharacterIndexForPoint(location, TextView.TextContainer, ref _);

            if (termsOfServiceTextRange.ContainsNumber(characterIndex))
                ViewModel.ViewTermsOfService.Execute();

            if (privacyPolicyRange.ContainsNumber(characterIndex))
                ViewModel.ViewPrivacyPolicy.Execute();
        }
    }
}
