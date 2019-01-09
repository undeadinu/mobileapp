using System;
using System.Collections.Generic;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Toggl.Daneel
{
    public class ReportsCalendarCollectionViewLayout : UICollectionViewLayout
    {
        public const float CellHeight = 42;
        private const int columnCount = 7;
        private nfloat cellWidth;
        private UICollectionViewLayoutAttributes[] attributes;

        public override CGSize CollectionViewContentSize
            => new CGSize(
                width: CollectionView.NumberOfSections() * CollectionView.Frame.Width,
                height: CollectionView.Frame.Height);

        public override void PrepareLayout()
        {
            cellWidth = CollectionView.Frame.Width / columnCount;
        }

        public override UICollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath indexPath)
        {
            var cellAttributes = UICollectionViewLayoutAttributes.CreateForCell(indexPath);

            var column = indexPath.Item % columnCount;
            var row = (int)indexPath.Item / columnCount;

            var frame = new CGRect
            {
                X = cellWidth * column + CollectionView.Frame.Width * indexPath.Section,
                Y = CellHeight * row,
                Width = cellWidth,
                Height = CellHeight
            };

            cellAttributes.Frame = frame;

            return cellAttributes;
        }

        public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(CGRect rect)
        {
            if (attributes != null)
                return attributes;

            var sectionCount = CollectionView.NumberOfSections();
            var result = new List<UICollectionViewLayoutAttributes>();
            for (int i = 0; i < sectionCount; i++)
            {
                var itemCount = CollectionView.NumberOfItemsInSection(i);
                for (int j = 0; j < itemCount; j++)
                {
                    var index = NSIndexPath.FromItemSection(j, i);
                    result.Add(LayoutAttributesForItem(index));
                }
            }

            return attributes = result.ToArray();
        }

        public override void InvalidateLayout()
        {
            base.InvalidateLayout();
            attributes = null;
        }
    }
}
