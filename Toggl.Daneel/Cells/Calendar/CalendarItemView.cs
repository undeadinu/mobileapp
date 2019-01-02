using System;
using System.Collections.Generic;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.UI;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Views;
using Toggl.Daneel.Views.Calendar;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.MvvmCross.Extensions;
using UIKit;

namespace Toggl.Daneel.Cells.Calendar
{
    public sealed partial class CalendarItemView : ReactiveCollectionViewCell<CalendarItem>
    {
        private static readonly Dictionary<CalendarIconKind, UIImage> images;

        private CALayer backgroundLayer;
        private CALayer patternLayer;
        private CALayer tintLayer;
        private CAShapeLayer topBorderLayer;
        private CAShapeLayer bottomBorderLayer;
        private CAShapeLayer topDragIndicatorBorderLayer;
        private CAShapeLayer bottomDragIndicatorBorderLayer;

        public static readonly NSString Key = new NSString(nameof(CalendarItemView));
        public static readonly UINib Nib;

        public CGRect TopDragTouchArea => TopDragIndicator.Frame.Inset(-20, -20);
        public CGRect BottomDragTouchArea => BottomDragIndicator.Frame.Inset(-20, -20);

        //In other words: 30 minutes or less
        private bool shortCalendarItem => Frame.Height <= CalendarCollectionViewLayout.HourHeight / 2;

        private bool shouldCenterIconVertically => Frame.Height <= CalendarCollectionViewLayout.HourHeight / 4;

        private bool isEditing;
        public bool IsEditing
        {
            get => isEditing;
            set
            {
                isEditing = value;
                updateDragIndicators(itemColor());
                updateShadow();
            }
        }

        private bool isRunningTimeEntry => Item.Duration == null;

        static CalendarItemView()
        {
            Nib = UINib.FromName(nameof(CalendarItemView), NSBundle.MainBundle);

            images = new Dictionary<CalendarIconKind, UIImage>
            {
                { CalendarIconKind.Unsynced, templateImage("icUnsynced") },
                { CalendarIconKind.Event, templateImage("icCalendarSmall") },
                { CalendarIconKind.Unsyncable, templateImage("icErrorSmall") }
            };

            UIImage templateImage(string iconName)
                => UIImage.FromBundle(iconName)
                      .ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
        }

        public CalendarItemView(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            prepareInitialConstraints();

            backgroundLayer = new CALayer();
            patternLayer = new CALayer { CornerRadius = 2 };
            tintLayer = new CALayer();
            topBorderLayer = new CAShapeLayer();
            bottomBorderLayer = new CAShapeLayer();

            ContentView.Layer.InsertSublayer(topBorderLayer, 0);
            ContentView.Layer.InsertSublayer(bottomBorderLayer, 1);
            ContentView.Layer.InsertSublayer(backgroundLayer, 2);
            ContentView.Layer.InsertSublayer(patternLayer, 3);
            ContentView.Layer.InsertSublayer(tintLayer, 4);

            ContentView.BringSubviewToFront(TopDragIndicator);
            ContentView.BringSubviewToFront(BottomDragIndicator);

            topDragIndicatorBorderLayer = new CAShapeLayer();
            configureDragIndicatorBorderLayer(TopDragIndicator, topDragIndicatorBorderLayer);
            bottomDragIndicatorBorderLayer = new CAShapeLayer();
            configureDragIndicatorBorderLayer(BottomDragIndicator, bottomDragIndicatorBorderLayer);

            void configureDragIndicatorBorderLayer(UIView dragIndicator, CAShapeLayer borderLayer)
            {
                var rect = dragIndicator.Bounds;
                borderLayer.Path = UIBezierPath.FromOval(rect).CGPath;
                borderLayer.BorderWidth = 2;
                borderLayer.FillColor = UIColor.Clear.CGColor;
                dragIndicator.Layer.AddSublayer(borderLayer);
            }
        }

        protected override void UpdateView()
        {
            var color = itemColor();
            backgroundLayer.BackgroundColor = UIColor.White.CGColor;
            patternLayer.BackgroundColor = patternColor(Item.Source, color).CGColor;
            tintLayer.BackgroundColor = tintColor(color).CGColor;
            BackgroundColor = UIColor.White;
            DescriptionLabel.Text = Item.Description;
            DescriptionLabel.TextColor = textColor(color);

            updateIcon(color);
            updateConstraints();
            updateBorderStyle(color);
            updateDragIndicators(color);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            updateShadow();
            updateBorderLayers();
            updateBackgroundLayers();
            updateConstraints();
        }

        private UIColor itemColor()
            => MvxColor.ParseHexString(Item.Color).ToNativeColor();

        private UIColor patternColor(CalendarItemSource source, UIColor color)
        {
            switch (source)
            {
                case CalendarItemSource.Calendar:
                    return color.ColorWithAlpha((nfloat)0.24);
                case CalendarItemSource.TimeEntry:
                    if (isRunningTimeEntry)
                    {
                        var patternTint = color.ColorWithAlpha((nfloat)0.1);
                        var patternTemplate = UIImage.FromBundle("stripes").ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);

                        UIGraphics.BeginImageContextWithOptions(patternTemplate.Size, false, patternTemplate.CurrentScale);
                        UIGraphics.GetCurrentContext().ScaleCTM(1, -1);
                        UIGraphics.GetCurrentContext().TranslateCTM(0, -patternTemplate.Size.Height);
                        patternTint.SetColor();
                        patternTemplate.Draw(new CGRect(0, 0, patternTemplate.Size.Width, patternTemplate.Size.Height));

                        var pattern = UIGraphics.GetImageFromCurrentImageContext();
                        UIGraphics.EndImageContext();

                        return UIColor.FromPatternImage(pattern);
                    }
                    else
                    {
                        return color;
                    }
                default:
                    throw new ArgumentException("Unexpected calendar item source");
            }
        }

        private UIColor tintColor(UIColor color)
            => isRunningTimeEntry ? color.ColorWithAlpha((nfloat)0.04) : UIColor.Clear;

        private UIColor textColor(UIColor color)
        {
            switch (Item.Source)
            {
                case CalendarItemSource.Calendar:
                    return color;
                case CalendarItemSource.TimeEntry:
                    return isRunningTimeEntry ? color : Item.ForegroundColor().ToNativeColor();
                default:
                    throw new ArgumentException("Unexpected calendar item source");
            }
        }

        private void updateIcon(UIColor color)
        {
            if (Item.IconKind == CalendarIconKind.None)
            {
                CalendarIconImageView.Hidden = true;
                return;
            }

            CalendarIconImageView.Hidden = false;
            CalendarIconImageView.TintColor = textColor(color);
            CalendarIconImageView.Image = images[Item.IconKind];
        }

        private void updateDragIndicators(UIColor color)
        {
            TopDragIndicator.Hidden = !IsEditing;
            BottomDragIndicator.Hidden = !IsEditing || isRunningTimeEntry;
            topDragIndicatorBorderLayer.StrokeColor = color.CGColor;
            bottomDragIndicatorBorderLayer.StrokeColor = color.CGColor;
        }

        private void prepareInitialConstraints()
        {
            // The following constraints are required because there's an undocumented change in iOS 12
            ContentView.TranslatesAutoresizingMaskIntoConstraints = false;
            ContentView.TopAnchor.ConstraintEqualTo(TopAnchor).Active = true;
            ContentView.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;
            ContentView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor).Active = true;
            ContentView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor).Active = true;
        }

        private void updateConstraints()
        {
            CalendarIconWidthConstrarint.Constant
                = CalendarIconHeightConstrarint.Constant
                = iconSize();

            CalendarIconBaselineConstraint.Active = !shouldCenterIconVertically;
            CalendarIconCenterVerticallyConstraint.Active = shouldCenterIconVertically;

            DescriptionLabelLeadingConstraint.Constant = descriptionLabelLeadingConstraintConstant();
            DescriptionLabelTopConstraint.Constant
                = DescriptionLabelBottomConstraint.Constant
                = descriptionLabelTopAndBottomConstraintConstant();

        }

        private void updateBorderStyle(UIColor color)
        {
            topBorderLayer.FillColor = UIColor.Clear.CGColor;
            topBorderLayer.StrokeColor = isRunningTimeEntry ? color.CGColor : UIColor.Clear.CGColor;
            topBorderLayer.LineWidth = 1.5f;
            topBorderLayer.LineCap = CAShapeLayer.CapRound;

            bottomBorderLayer.FillColor = UIColor.Clear.CGColor;
            bottomBorderLayer.StrokeColor = isRunningTimeEntry ? color.CGColor : UIColor.Clear.CGColor;
            bottomBorderLayer.LineWidth = 1.5f;
            bottomBorderLayer.LineCap = CAShapeLayer.CapRound;
            bottomBorderLayer.LineDashPattern = new NSNumber[] { 4, 6 };
        }

        private void updateBackgroundLayers()
        {
            CATransaction.Begin();
            CATransaction.AnimationDuration = 0.0;

            var borderWidth = isRunningTimeEntry ? 1.5 : 0;
            var rect = ContentView.Bounds.Inset((nfloat)borderWidth, (nfloat)borderWidth);

            backgroundLayer.Frame = rect;
            patternLayer.Frame = rect;
            tintLayer.Frame = rect;

            CATransaction.Commit();
        }

        private void updateBorderLayers()
        {
            var dashLineHeight = CalendarCollectionViewLayout.HourHeight / 4;
            var halfLineWidth = 0.5;

            CATransaction.Begin();
            CATransaction.AnimationDuration = 0.0;

            topBorderLayer.Frame = new CGRect(0, 0, ContentView.Bounds.Width, ContentView.Bounds.Height - dashLineHeight);
            bottomBorderLayer.Frame = new CGRect(0, ContentView.Bounds.Height - dashLineHeight, ContentView.Bounds.Width, dashLineHeight);

            var topBorderBezierPathRect = topBorderLayer.Bounds.Inset((nfloat)halfLineWidth, (nfloat)halfLineWidth);
            var topBorderBezierPath = UIBezierPath.FromRoundedRect(topBorderBezierPathRect, UIRectCorner.TopLeft | UIRectCorner.TopRight, new CGSize(2, 2));
            topBorderLayer.Path = topBorderBezierPath.CGPath;

            var bottomBorderBezierPathRect = bottomBorderLayer.Bounds.Inset((nfloat)halfLineWidth, (nfloat)halfLineWidth);
            var bottomBorderBezierPath = UIBezierPath.FromRoundedRect(bottomBorderBezierPathRect, UIRectCorner.BottomLeft | UIRectCorner.BottomRight, new CGSize(2, 2));
            bottomBorderLayer.Path = bottomBorderBezierPath.CGPath;

            CATransaction.Commit();
        }

        private int descriptionLabelLeadingConstraintConstant()
        {
            if (Item.IconKind == CalendarIconKind.None)
                return 5;

            return shortCalendarItem ? 18 : 24;
        }

        private int iconSize()
            => shortCalendarItem ? 8 : 14;

        private int descriptionLabelTopAndBottomConstraintConstant()
            => shortCalendarItem ? 0 : 6;

        private void updateShadow()
        {
            if (isEditing)
            {
                var shadowPath = UIBezierPath.FromRect(Bounds);
                Layer.ShadowPath?.Dispose();
                Layer.ShadowPath = shadowPath.CGPath;

                Layer.CornerRadius = 2;
                Layer.ShadowRadius = 4;
                Layer.ShadowOpacity = 0.1f;
                Layer.MasksToBounds = false;
                Layer.ShadowOffset = new CGSize(0, 4);
                Layer.ShadowColor = UIColor.Black.CGColor;
            }
            else
            {
                Layer.ShadowOpacity = 0;
            }
        }
    }
}
