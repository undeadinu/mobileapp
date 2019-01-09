using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreGraphics;
using Foundation;
using MvvmCross.Binding.Extensions;
using MvvmCross.Platforms.Ios.Binding.Views;
using Toggl.Daneel.Views.Reports;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts;
using Toggl.Multivac;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class ReportsCalendarQuickSelectCollectionViewSource
        : MvxCollectionViewSource, IUICollectionViewDelegateFlowLayout
    {
        private const int cellWidth = 96;
        private const int cellHeight = 32;
        private const string cellIdentifier = nameof(ReportsCalendarQuickSelectViewCell);

        private List<ReportsCalendarBaseQuickSelectShortcut> shortcuts = new List<ReportsCalendarBaseQuickSelectShortcut>();

        private ISubject<ReportsCalendarBaseQuickSelectShortcut> shortcutTaps = new Subject<ReportsCalendarBaseQuickSelectShortcut>();

        public IObservable<ReportsCalendarBaseQuickSelectShortcut> ShortcutTaps;

        private ReportsDateRangeParameter currentDateRange;
        private NSIndexPath selectedIndexPath;

        public ReportsCalendarQuickSelectCollectionViewSource(
            UICollectionView collectionView) : base(collectionView)
        {
            Ensure.Argument.IsNotNull(collectionView, nameof(collectionView));
            collectionView.RegisterNibForCell(ReportsCalendarQuickSelectViewCell.Nib, cellIdentifier);
            ShortcutTaps = shortcutTaps.AsObservable();
        }

        protected override UICollectionViewCell GetOrCreateCellFor(UICollectionView collectionView, NSIndexPath indexPath, object item)
            => collectionView.DequeueReusableCell(cellIdentifier, indexPath) as UICollectionViewCell;

        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var item = GetItemAt(indexPath) as ReportsCalendarBaseQuickSelectShortcut;
            var cell = GetOrCreateCellFor(collectionView, indexPath, item) as ReportsCalendarQuickSelectViewCell;

            cell.UpdateSelectedDateRange(currentDateRange);
            cell.Item = item;

            return cell;
        }

        public override nint NumberOfSections(UICollectionView collectionView) => 1;

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
            => shortcuts.Count();

        protected override object GetItemAt(NSIndexPath indexPath)
            => shortcuts.ElementAt((int) indexPath.Item);

        [Export("collectionView:layout:sizeForItemAtIndexPath:")]
        public CGSize GetSizeForItem(UICollectionView collectionView, UICollectionViewLayout layout, NSIndexPath indexPath)
        {
            return new CGSize(cellWidth, cellHeight);
        }

        public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
        {
            shortcutTaps.OnNext(shortcuts[indexPath.Row]);
        }

        public void UpdateShortcuts(List<ReportsCalendarBaseQuickSelectShortcut> newShortcuts)
        {
            shortcuts = newShortcuts;
            ReloadData();
        }

        public void UpdateSelection(ReportsDateRangeParameter dateRange)
        {
            currentDateRange = dateRange;
            ReloadData();
        }
    }
}
