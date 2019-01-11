using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CoreGraphics;
using Foundation;
using MvvmCross.Commands;
using MvvmCross.Platforms.Ios.Binding.Views;
using ObjCRuntime;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;
using static Toggl.Multivac.Extensions.CommonFunctions;

namespace Toggl.Daneel
{
    public partial class RatingView : MvxView
    {
        private readonly UIStringAttributes descriptionStringAttributes = new UIStringAttributes
        {
            ParagraphStyle = new NSMutableParagraphStyle
            {
                MaximumLineHeight = 22,
                MinimumLineHeight = 22,
                Alignment = UITextAlignment.Center,
            }
        };

        private NSLayoutConstraint heightConstraint;

        public IMvxCommand CTATappedCommand { get; set; }
        public IMvxCommand DismissTappedCommand { get; set; }
        public IMvxCommand<bool> ImpressionTappedCommand { get; set; }

        public new RatingViewModel DataContext
        {
            get => base.DataContext as RatingViewModel;
            set
            {
                base.DataContext = value;
                updateBindings();
            }
        }

        public CompositeDisposable DisposeBag { get; } = new CompositeDisposable();

        public RatingView (IntPtr handle) : base (handle)
        {
        }

        public static RatingView Create()
        {
            var arr = NSBundle.MainBundle.LoadNib(nameof(RatingView), null, null);
            return Runtime.GetNSObject<RatingView>(arr.ValueAt(0));
        }

        private void updateBindings()
        {
            DataContext.CallToActionTitle
                .Subscribe(CtaTitle.Rx().Text())
                .DisposedBy(DisposeBag);

            DataContext.CallToActionButtonTitle
                .Subscribe(CtaButton.Rx().Title())
                .DisposedBy(DisposeBag);

            DataContext.Impression
                .Select(impression => impression.HasValue)
                .Subscribe(CtaView.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);

            DataContext.CallToActionDescription.Select(attributedDescription)
                .Subscribe(CtaDescription.Rx().AttributedText())
                .DisposedBy(DisposeBag);

            DataContext.Impression
                .Select(impression => impression.HasValue)
                .Select(Invert)
                .Subscribe(QuestionView.Rx().IsVisibleWithFade())
                .DisposedBy(DisposeBag);

            DataContext.Impression
                .Select(impression => impression.HasValue)
                .Subscribe(CtaViewBottomConstraint.Rx().Active())
                .DisposedBy(DisposeBag);

            DataContext.Impression
                .Select(impression => impression.HasValue)
                .Select(Invert)
                .Subscribe(QuestionViewBottomConstraint.Rx().Active())
                .DisposedBy(DisposeBag);

            YesView.Rx().Tap()
                .Subscribe(() => DataContext.RegisterImpression(true))
                .DisposedBy(DisposeBag);

            NotReallyView.Rx().Tap()
                .Subscribe(() => DataContext.RegisterImpression(false))
                .DisposedBy(DisposeBag);

            CtaButton.Rx()
                .BindAction(DataContext.PerformMainAction)
                .DisposedBy(DisposeBag);

            DismissButton.Rx().Tap()
                .Subscribe(DataContext.Dismiss)
                .DisposedBy(DisposeBag);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            DisposeBag.Dispose();
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            CtaButton.SetTitle(Resources.RatingCallToActionTitle, UIControlState.Normal);
            DismissButton.SetTitle(Resources.NoThanks, UIControlState.Normal);

            SetupAsCard(QuestionView);
            SetupAsCard(CtaView);

            CtaButton.Layer.CornerRadius = 8;
            CtaView.Layer.MasksToBounds = false;
        }

        private NSAttributedString attributedDescription(string text)
            => new NSAttributedString(text, descriptionStringAttributes);

        private void SetupAsCard(UIView view)
        {
            var shadowPath = UIBezierPath.FromRect(view.Bounds);
            view.Layer.ShadowPath?.Dispose();
            view.Layer.ShadowPath = shadowPath.CGPath;

            view.Layer.CornerRadius = 8;
            view.Layer.ShadowRadius = 4;
            view.Layer.ShadowOpacity = 0.1f;
            view.Layer.MasksToBounds = false;
            view.Layer.ShadowOffset = new CGSize(0, 2);
            view.Layer.ShadowColor = UIColor.Black.CGColor;
        }
    }
}
