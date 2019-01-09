using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Toggl.Multivac.Extensions;
using static Android.Views.MotionEventActions;

namespace Toggl.Giskard.Views
{
    [Register("com.toggl.giskard.ReportsLinearLayout")]
    public sealed class ReportsLinearLayout : LinearLayout, GestureDetector.IOnGestureListener
    {
        private const float flingVelocityThreshold = 1500;
        private const int calendarAnimationDuration = 500;
        private const int calendarAnimationFlingDuration = 200;

        private float initialY;
        private float currentX;
        private float currentY;
        private View calendarContainer;
        private int negativeContainerHeight;
        private readonly GestureDetector gestureDetector;

        internal View CalendarContainer
        {
            get => calendarContainer;
            set
            {
                calendarContainer = value;

                value.Post(() =>
                {
                    negativeContainerHeight = -calendarContainer.Height;
                    animateCalendar(true, true);
                });
            }
        }

        public ReportsLinearLayout(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public ReportsLinearLayout(Context context) :
            base(context)
        {
            gestureDetector = new GestureDetector(Context, this);
        }

        public ReportsLinearLayout(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            gestureDetector = new GestureDetector(Context, this);
        }

        public ReportsLinearLayout(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            gestureDetector = new GestureDetector(Context, this);
        }

        internal void ToggleCalendar(bool forceHide)
        {
            animateCalendar(forceHide, false);
        }

        internal void RecalculateCalendarHeight()
        {
            Post(() =>
            {
                negativeContainerHeight = -calendarContainer.Height;

                var marginParams = CalendarContainer.LayoutParameters as MarginLayoutParams;
                var calendarIsTotallyVisible = marginParams.TopMargin == 0;
                if (calendarIsTotallyVisible)
                    return;

                marginParams.TopMargin = negativeContainerHeight;
                CalendarContainer.LayoutParameters = marginParams;
            });
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            if (ev.Action == Down)
            {
                currentX = ev.RawX;
                currentY = ev.RawY;
                initialY = currentY;
                return false;
            }

            int[] outLocation = new int[2];
            GetLocationOnScreen(outLocation);
            var minValidY = outLocation[1] + Math.Abs(negativeContainerHeight);
            var isScrollingInsideCalendar = initialY <= minValidY;
            if (isScrollingInsideCalendar)
                return false;

            if (ev.Action != Move)
                return false;

            var marginParams = CalendarContainer.LayoutParameters as MarginLayoutParams;

            var offsetY = ev.RawY - currentY;
            var offsetX = ev.RawX - currentX;
            var calendarIsTotallyVisible = marginParams.TopMargin == 0;
            var calendarIsTotallyHidden = marginParams.TopMargin <= negativeContainerHeight;

            var isSwipingUp = offsetY < 0;
            var isSwipingDown = !isSwipingUp;

            var shouldIntercept =
                !(calendarIsTotallyVisible && offsetX > offsetY) &&
                (!calendarIsTotallyHidden && !calendarIsTotallyVisible) ||
                (calendarIsTotallyVisible && isSwipingUp);

            return shouldIntercept;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            var flingHandled = gestureDetector.OnTouchEvent(e);
            if (flingHandled)
                return true;

            switch (e.Action)
            {
                case Down:
                    currentY = e.RawY;
                    return true;

                case Up:
                    currentY = 0;
                    return true;

                case Move:

                    var newY = e.RawY;
                    var offset = newY - currentY;
                    currentY = newY;

                    var marginParams = CalendarContainer.LayoutParameters as MarginLayoutParams;
                    var calendarIsTotallyVisible = marginParams.TopMargin == 0;
                    var calendarIsTotallyHidden = marginParams.TopMargin == negativeContainerHeight;

                    var isSwipingUp = offset < 0;
                    var isSwipingDown = !isSwipingUp;

                    var shouldReturn =
                        calendarIsTotallyHidden && isSwipingUp ||
                        calendarIsTotallyVisible && isSwipingDown;

                    if (shouldReturn)
                        return false;

                    var oldMargin = marginParams.TopMargin;
                    var newMargin = (oldMargin + (int)offset).Clamp(negativeContainerHeight, 0);
                    marginParams.TopMargin = newMargin;
                    CalendarContainer.LayoutParameters = marginParams;
                    return true;
            }

            return base.OnTouchEvent(e);
        }

        public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            if (Math.Abs(velocityY) > Math.Abs(velocityX) && velocityY <= flingVelocityThreshold)
            {
                animateCalendar(true, true);
                return true;
            }

            return false;
        }

        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY) => false;

        public bool OnSingleTapUp(MotionEvent e) => false;

        public bool OnDown(MotionEvent e) => false;

        public void OnLongPress(MotionEvent e) { }

        public void OnShowPress(MotionEvent e) { }

        private void animateCalendar(bool forceHide, bool isFling)
        {
            var marginParams = CalendarContainer.LayoutParameters as MarginLayoutParams;
            var isHidding = forceHide || marginParams.TopMargin != negativeContainerHeight;

            if (isHidding && marginParams.TopMargin < negativeContainerHeight)
                return;

            var newMargin = isHidding ? negativeContainerHeight : 0;
            var animation = new TopMarginAnimation(CalendarContainer, newMargin, isHidding);
            animation.Duration = isFling ? calendarAnimationFlingDuration : calendarAnimationDuration;

            void notifyShowAnimationEndToCalendarContainer(object sender, Animation.AnimationEndEventArgs e)
            {
                if (!isHidding)
                {
                    ((ReportsCalendarView) CalendarContainer).ExecutePendingPageUpdate();
                }
            }

            animation.AnimationEnd += notifyShowAnimationEndToCalendarContainer;
            CalendarContainer.StartAnimation(animation);
            CalendarContainer.RequestLayout();
            Invalidate();
        }

        private class TopMarginAnimation : Animation
        {
            private readonly View view;
            private readonly int diff;
            private readonly int newMargin;
            private readonly int initialMargin;

            public TopMarginAnimation(View view, int newMargin, bool isHidding)
            {
                this.view = view;
                this.newMargin = newMargin;

                var marginParams = view.LayoutParameters as MarginLayoutParams;

                initialMargin = marginParams.TopMargin;
                diff = Math.Abs(initialMargin - newMargin) * (isHidding ? -1 : 1);
            }

            protected override void ApplyTransformation(float interpolatedTime, Transformation t)
            {
                var marginParams = view.LayoutParameters as MarginLayoutParams;
                marginParams.TopMargin = initialMargin + (int)(diff * interpolatedTime);
                view.LayoutParameters = marginParams;
            }
        }
    }
}
