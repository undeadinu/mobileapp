using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Foundation;
using MvvmCross.Commands;
using MvvmCross.Platforms.Ios.Binding.Views;
using Toggl.Daneel.Views;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class ReportsCalendarCollectionViewSource : MvxCollectionViewSource
    {
        private const string cellIdentifier = nameof(ReportsCalendarViewCell);

        private ISubject<ReportsCalendarDayViewModel> dayTaps = new Subject<ReportsCalendarDayViewModel>();
        
        private List<ReportsCalendarPageViewModel> months = new List<ReportsCalendarPageViewModel>();

        private ReportsDateRangeParameter currentSelectedDateRange;

        private readonly ISubject<int> decelerationEndedSubject = new Subject<int>();
        
        public readonly IObservable<ReportsCalendarDayViewModel> DayTaps;

        public IObservable<int> DecelerationEndedObservable { get; }


        public ReportsCalendarCollectionViewSource(UICollectionView collectionView)
            : base(collectionView)
        {
            collectionView.RegisterNibForCell(ReportsCalendarViewCell.Nib, cellIdentifier);
            DayTaps = dayTaps.AsObservable();
            DecelerationEndedObservable = decelerationEndedSubject.AsObservable();
        }

        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var item = GetItemAt(indexPath);
            var cell = GetOrCreateCellFor(collectionView, indexPath, item) as ReportsCalendarViewCell;

            cell.UpdateDateRange(currentSelectedDateRange);
            cell.Item = months[indexPath.Section].Days[indexPath.Row];
            return cell;
        }

        public void UpdateMonths(List<ReportsCalendarPageViewModel> newMonths)
        {
            months = newMonths;
            if (currentSelectedDateRange != null)
            {
                UpdateSelection(currentSelectedDateRange);
            }
            else
            {
                CollectionView.ReloadData();
            }
        }

        protected override UICollectionViewCell GetOrCreateCellFor(UICollectionView collectionView, NSIndexPath indexPath, object item)
            => collectionView.DequeueReusableCell(cellIdentifier, indexPath) as UICollectionViewCell;

        public void UpdateSelection(ReportsDateRangeParameter selectedDateRange)
        {
            if (selectedDateRange == null) return;

            currentSelectedDateRange = selectedDateRange;
            CollectionView.CollectionViewLayout.InvalidateLayout();
            CollectionView.ReloadData();
        }

        public override nint NumberOfSections(UICollectionView collectionView)
            => months.Count;

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
            => months[(int) section].Days.Count;

        protected override object GetItemAt(NSIndexPath indexPath)
            => months[indexPath.Section].Days[(int) indexPath.Item];

        public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
        {
            dayTaps.OnNext(months[indexPath.Section].Days[indexPath.Row]);
        }

        public override void DecelerationEnded(UIScrollView scrollView)
        {
            decelerationEndedSubject.OnNext((int) (CollectionView.ContentOffset.X / CollectionView.Frame.Width));
        }
    }
}
