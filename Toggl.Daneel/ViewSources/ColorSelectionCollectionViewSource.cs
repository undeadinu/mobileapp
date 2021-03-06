﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;
using CoreGraphics;
using Foundation;
using MvvmCross.UI;
using Toggl.Daneel.Views;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class ColorSelectionCollectionViewSource : ListCollectionViewSource<SelectableColorViewModel, ColorSelectionViewCell>, IUICollectionViewDelegateFlowLayout
    {
        public IObservable<MvxColor> ColorSelected
            => Observable
                .FromEventPattern<SelectableColorViewModel>(e => OnItemTapped += e, e => OnItemTapped -= e)
                .Select(e => e.EventArgs.Color);

        public ColorSelectionCollectionViewSource(IObservable<IEnumerable<SelectableColorViewModel>> colors)
            : base (new List<SelectableColorViewModel>().ToImmutableList(), ColorSelectionViewCell.Identifier)
        {

        }

        public void SetNewColors(IEnumerable<SelectableColorViewModel> colors)
        {
            items = colors.ToImmutableList();
        }

        [Export("collectionView:layout:sizeForItemAtIndexPath:")]
        public CGSize GetSizeForItem(
            UICollectionView collectionView, UICollectionViewLayout layout, NSIndexPath indexPath)
            => new CGSize(Math.Floor(collectionView.Frame.Width / 5), 36);

        [Export("collectionView:layout:minimumLineSpacingForSectionAtIndex:")]
        public nfloat GetMinimumLineSpacingForSection(
            UICollectionView collectionView, UICollectionViewLayout layout, nint section)
            => 12;

        [Export("collectionView:layout:minimumInteritemSpacingForSectionAtIndex:")]
        public nfloat GetMinimumInteritemSpacingForSection(
            UICollectionView collectionView, UICollectionViewLayout layout, nint section)
            => 0;

        [Export("collectionView:layout:insetForSectionAtIndex:")]
        public UIEdgeInsets GetInsetForSection(
            UICollectionView collectionView, UICollectionViewLayout layout, nint section)
            => new UIEdgeInsets(0, 0, 0, 0);
    }
}
