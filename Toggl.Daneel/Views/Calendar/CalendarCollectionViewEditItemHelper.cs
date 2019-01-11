using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreGraphics;
using Foundation;
using Toggl.Daneel.Cells.Calendar;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Helper;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using UIKit;
using Math = System.Math;

namespace Toggl.Daneel.Views.Calendar
{
    public sealed class CalendarCollectionViewEditItemHelper : CalendarCollectionViewAutoScrollHelper, IUIGestureRecognizerDelegate
    {
        enum EditAction
        {
            ChangeOffset,
            ChangeStartTime,
            ChangeEndTime,
            None
        }

        private static readonly TimeSpan defaultDuration = Constants.CalendarItemViewDefaultDuration;

        private readonly ITimeService timeService;
        private readonly CalendarCollectionViewSource dataSource;

        private UILongPressGestureRecognizer longPressGestureRecognizer;
        private UIPanGestureRecognizer panGestureRecognizer;
        private UITapGestureRecognizer tapGestureRecognizer;

        private CalendarItem calendarItem;

        private NSIndexPath itemIndexPath;
        private nfloat verticalOffset;
        private CGPoint firstPoint;
        private CGPoint previousPoint;

        private bool isActive;
        private EditAction action;

        private bool didDragUp;
        private bool didDragDown;

        private readonly ISubject<CalendarItem> editCalendarItemSuject = new Subject<CalendarItem>();
        private readonly ISubject<CalendarItem> longPressCalendarEventSubject = new Subject<CalendarItem>();

        public IObservable<CalendarItem> EditCalendarItem => editCalendarItemSuject.AsObservable();
        public IObservable<CalendarItem> LongPressCalendarEvent => longPressCalendarEventSubject.AsObservable();

        public CalendarCollectionViewEditItemHelper(
            UICollectionView CollectionView,
            ITimeService timeService,
            CalendarCollectionViewSource dataSource,
            CalendarCollectionViewLayout Layout) : base(CollectionView, Layout)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.timeService = timeService;
            this.dataSource = dataSource;

            longPressGestureRecognizer = new UILongPressGestureRecognizer(onLongPress);
            longPressGestureRecognizer.Delegate = this;
            CollectionView.AddGestureRecognizer(longPressGestureRecognizer);

            panGestureRecognizer = new UIPanGestureRecognizer(onPan);
            panGestureRecognizer.Delegate = this;

            tapGestureRecognizer = new UITapGestureRecognizer(onTap);
            tapGestureRecognizer.Delegate = this;
        }

        public CalendarCollectionViewEditItemHelper(IntPtr handle) : base(handle)
        {
        }

        [Export("gestureRecognizer:shouldRecognizeSimultaneouslyWithGestureRecognizer:")]
        public bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)
        {
            if (gestureRecognizer == longPressGestureRecognizer)
                return otherGestureRecognizer is UILongPressGestureRecognizer;
            else
                return false;
        }

        private void onLongPress(UILongPressGestureRecognizer gesture)
        {
            var point = gesture.LocationInView(CollectionView);

            switch (gesture.State)
            {
                case UIGestureRecognizerState.Began:
                    longPressBegan(point);
                    break;

                case UIGestureRecognizerState.Changed:
                    longPressChanged(point);
                    break;
                case UIGestureRecognizerState.Ended:
                    longPressEnded();
                    break;
            }
        }

        private void onPan(UIPanGestureRecognizer gesture)
        {
            var point = gesture.LocationInView(CollectionView);

            switch (gesture.State)
            {
                case UIGestureRecognizerState.Began:
                    panBegan(point);
                    break;

                case UIGestureRecognizerState.Changed:
                    panChanged(point);
                    break;

                case UIGestureRecognizerState.Ended:
                    panEnded();
                    break;
            }
        }

        private void onTap(UITapGestureRecognizer gesture)
        {
            var point = gesture.LocationInView(CollectionView);

            resignActive();
            dataSource.StopEditing();
            editCalendarItemSuject.OnNext(calendarItem);

            impactFeedback.ImpactOccurred();
        }

        private void longPressBegan(CGPoint point)
        {
            if (dataSource.IsEditing || isActive)
                return;

            var itemAtPoint = dataSource.CalendarItemAtPoint(point);
            if (!itemAtPoint.HasValue)
                return;

            calendarItem = itemAtPoint.Value;

            if (calendarItem.Source == CalendarItemSource.Calendar)
            {
                longPressCalendarEventSubject.OnNext(calendarItem);
                return;
            }

            if (!calendarItem.IsEditable())
                return;

            itemIndexPath = CollectionView.IndexPathForItemAtPoint(point);
            dataSource.StartEditing(itemIndexPath);
            becomeActive();

            var startPoint = Layout.PointAtDate(calendarItem.StartTime.ToLocalTime());
            firstPoint = point;
            LastPoint = point;
            previousPoint = point;
            verticalOffset = firstPoint.Y - startPoint.Y;

            impactFeedback.ImpactOccurred();
            selectionFeedback.Prepare();
        }

        private void longPressChanged(CGPoint point)
        {
            onCurrentPointChanged(point);
            changeOffset(point);
            previousPoint = point;
        }

        private void longPressEnded()
        {
            StopAutoScroll();
            onCurrentPointChanged(null);
            if (!isActive)
                return;

            CollectionView.RemoveGestureRecognizer(longPressGestureRecognizer);
        }

        private void panBegan(CGPoint point)
        {
            if (!isActive)
                return;

            firstPoint = point;
            LastPoint = point;

            var cell = CollectionView.CellForItem(itemIndexPath) as CalendarItemView;
            var topDragRect = CollectionView.ConvertRectFromView(cell.TopDragTouchArea, cell);
            var bottomDragRect = CollectionView.ConvertRectFromView(cell.BottomDragTouchArea, cell);

            if (topDragRect.Contains(point))
                action = EditAction.ChangeStartTime;
            else if (bottomDragRect.Contains(point))
                action = EditAction.ChangeEndTime;
            else if (cell.Frame.Contains(point))
                action = EditAction.ChangeOffset;
            else
                action = EditAction.None;

            selectionFeedback.Prepare();
        }

        private void panChanged(CGPoint point)
        {
            onCurrentPointChanged(point);
            switch (action)
            {
                case EditAction.ChangeOffset:
                    changeOffset(point);
                    break;
                case EditAction.ChangeStartTime:
                    changeStartTime(point);
                    break;
                case EditAction.ChangeEndTime:
                    changeEndTime(point);
                    break;
            }
            previousPoint = point;
        }

        private void changeOffset(CGPoint point)
        {
            if (!isActive || itemIndexPath == null)
                return;

            if (Math.Abs(LastPoint.Y - point.Y) < CalendarCollectionViewLayout.HourHeight / 4)
                return;

            LastPoint = point;
            var now = timeService.CurrentDateTime;
            var startPoint = new CGPoint(LastPoint.X, LastPoint.Y - verticalOffset);
            var startTime = Layout.DateAtPoint(startPoint).ToLocalTime().RoundDownToClosestQuarter();

            if (startTime + calendarItem.Duration > startTime.Date.AddDays(1))
                return;

            calendarItem = calendarItem
                .WithStartTime(startTime);

            itemIndexPath = dataSource.UpdateItemView(itemIndexPath, calendarItem.StartTime, calendarItem.Duration(now));
            selectionFeedback.SelectionChanged();

            var topY = Layout.PointAtDate(calendarItem.StartTime).Y;
            var bottomY = Layout.PointAtDate(calendarItem.EndTime(now)).Y;
            if (topY < TopAutoScrollLine && !CollectionView.IsAtTop() && didDragUp)
                StartAutoScrollUp(changeOffset);
            else if (bottomY > BottomAutoScrollLine && !CollectionView.IsAtBottom() && didDragDown)
                StartAutoScrolDown(changeOffset);
            else
                StopAutoScroll();
        }

        private void changeStartTime(CGPoint point)
        {
            if (!isActive || itemIndexPath == null)
                return;

            if (Math.Abs(LastPoint.Y - point.Y) < CalendarCollectionViewLayout.HourHeight / 4)
                return;

            if (point.Y < 0 || point.Y >= Layout.ContentViewHeight)
                return;

            LastPoint = point;
            var now = timeService.CurrentDateTime;
            var startTime = Layout.DateAtPoint(LastPoint).ToLocalTime().RoundDownToClosestQuarter();
            var duration = calendarItem.Duration.HasValue ? calendarItem.EndTime(now) - startTime : null as TimeSpan?;

            if (duration != null && duration <= TimeSpan.Zero ||
                duration == null && startTime > now)
                return;

            calendarItem = calendarItem
                .WithStartTime(startTime)
                .WithDuration(duration);

            itemIndexPath = dataSource.UpdateItemView(itemIndexPath, calendarItem.StartTime, calendarItem.Duration(now));
            selectionFeedback.SelectionChanged();

            if (point.Y < TopAutoScrollLine && !CollectionView.IsAtTop())
                StartAutoScrollUp(changeStartTime);
            else if (point.Y > BottomAutoScrollLine && calendarItem.Duration > defaultDuration)
                StartAutoScrolDown(changeStartTime);
            else
                StopAutoScroll();
        }

        private void changeEndTime(CGPoint point)
        {
            if (calendarItem.Duration == null || !isActive || itemIndexPath == null)
                return;

            if (Math.Abs(LastPoint.Y - point.Y) < CalendarCollectionViewLayout.HourHeight / 4)
                return;

            if (point.Y < 0 || point.Y >= Layout.ContentViewHeight)
                return;

            LastPoint = point;
            var now = timeService.CurrentDateTime;
            var endTime = Layout.DateAtPoint(LastPoint).ToLocalTime().RoundUpToClosestQuarter();
            var duration = endTime - calendarItem.StartTime;

            if (duration <= TimeSpan.Zero)
                return;

            calendarItem = calendarItem
                .WithDuration(duration);

            itemIndexPath = dataSource.UpdateItemView(itemIndexPath, calendarItem.StartTime, calendarItem.Duration(now));
            selectionFeedback.SelectionChanged();

            if (point.Y > BottomAutoScrollLine && !CollectionView.IsAtBottom())
                StartAutoScrolDown(changeEndTime);
            else if (point.Y < TopAutoScrollLine && calendarItem.Duration > defaultDuration)
                StartAutoScrollUp(changeEndTime);
            else
                StopAutoScroll();
        }

        private void panEnded()
        {
            onCurrentPointChanged(null);
            StopAutoScroll();
        }

        private void becomeActive()
        {
            isActive = true;
            CollectionView.AddGestureRecognizer(panGestureRecognizer);
            CollectionView.AddGestureRecognizer(tapGestureRecognizer);
        }

        private void resignActive()
        {
            isActive = false;
            CollectionView.AddGestureRecognizer(longPressGestureRecognizer);
            CollectionView.RemoveGestureRecognizer(panGestureRecognizer);
            CollectionView.RemoveGestureRecognizer(tapGestureRecognizer);
        }

        private void onCurrentPointChanged(CGPoint? currentPoint)
        {
            if (currentPoint == null)
            {
                didDragUp = false;
                didDragDown = false;
                return;
            }

            if (currentPoint.Value.Y > previousPoint.Y)
            {
                didDragDown = true;
                didDragUp = false;
            }

            if (currentPoint.Value.Y < previousPoint.Y)
            {
                didDragUp = true;
                didDragDown = false;
            }
        }
    }
}
