using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Util;
using Android.Widget;
using MvvmCross.Plugin.Color.Platforms.Android;
using Toggl.Giskard.Extensions;
using static Toggl.Foundation.MvvmCross.Helper.Color;

namespace Toggl.Giskard.Views
{
    [Register("toggl.giskard.views.ReportsCalendarDayView")]
    public sealed class ReportsCalendarDayView : TextView
    {
        private readonly int cornerRadius;
        private readonly Paint circlePaint;
        private readonly Paint selectedPaint;
        private readonly int verticalPadding;

        private bool isToday;

        public bool IsToday
        {
            get => isToday;
            set
            {
                isToday = value;
                PostInvalidate();
            }
        }

        private bool isSelected;

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                PostInvalidate();
            }
        }

        private bool roundLeft;

        public bool RoundLeft
        {
            get => roundLeft;
            set
            {
                roundLeft = value;
                PostInvalidate();
            }
        }

        private bool roundRight;

        public bool RoundRight
        {
            get => roundRight;
            set
            {
                roundRight = value;
                PostInvalidate();
            }
        }

        public ReportsCalendarDayView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public ReportsCalendarDayView(Context context)
            : this(context, null)
        {
        }

        public ReportsCalendarDayView(Context context, IAttributeSet attrs)
            : this(context, attrs, 0)
        {
        }

        public ReportsCalendarDayView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            cornerRadius = (int) 22.DpToPixels(context);
            verticalPadding = (int) 6.DpToPixels(context);
            selectedPaint = new Paint
            {
                Flags = PaintFlags.AntiAlias,
                Color = new Color(ContextCompat.GetColor(context, Resource.Color.calendarSelected))
            };

            circlePaint = new Paint
            {
                Flags = PaintFlags.AntiAlias,
                Color = Reports.DayNotInMonth.ToNativeColor()
            };
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, widthMeasureSpec);
        }

        public override void Draw(Canvas canvas)
        {
            var width = Width;
            var height = Height - verticalPadding * 2;

            if (IsSelected)
            {
                var roundRect = new RectF(0, verticalPadding, width, height + verticalPadding);
                canvas.DrawRoundRect(roundRect, cornerRadius, cornerRadius, selectedPaint);

                var squareRectLeft = RoundLeft ? cornerRadius : 0;
                var squareRectRight = width - (RoundRight ? cornerRadius : 0);
                var squareRect = new RectF(squareRectLeft, verticalPadding, squareRectRight, height + verticalPadding);
                canvas.DrawRect(squareRect, selectedPaint);
            }
            else if (IsToday)
            {
                var centerX = width / 2;
                var centerY = height / 2 + verticalPadding;
                var radius = Math.Min(width, height) / 2;

                canvas.DrawCircle(centerX, centerY, radius, circlePaint);
            }

            base.Draw(canvas);
        }
    }
}
