using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreGraphics;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Views;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class TimeEntriesLogViewSource : ReactiveSectionedListTableViewSource<TimeEntryViewModel, TimeEntriesLogViewCell>
    {
        private const int rowHeight = 64;
        private const int headerHeight = 48;

        private readonly ITimeService timeService;
        private readonly ISchedulerProvider schedulerProvider;

        private readonly Subject<TimeEntryViewModel> continueTapSubject = new Subject<TimeEntryViewModel>();
        private readonly Subject<TimeEntryViewModel> swipeToContinueSubject = new Subject<TimeEntryViewModel>();
        private readonly Subject<TimeEntryViewModel> swipeToDeleteSubject = new Subject<TimeEntryViewModel>();
        private readonly ReplaySubject<TimeEntriesLogViewCell> firstCellSubject = new ReplaySubject<TimeEntriesLogViewCell>(1);

        //Using the old API so that delete action would work on pre iOS 11 devices
        private readonly UITableViewRowAction deleteTableViewRowAction;

        public const int SpaceBetweenSections = 20;

        public bool IsEmptyState { get; set; }

        public IObservable<TimeEntryViewModel> ContinueTap
            => continueTapSubject.AsObservable();

        public IObservable<TimeEntryViewModel> SwipeToContinue
            => swipeToContinueSubject.AsObservable();

        public IObservable<TimeEntryViewModel> SwipeToDelete
            => swipeToDeleteSubject.AsObservable();

        public IObservable<TimeEntriesLogViewCell> FirstCell { get; }

        public TimeEntriesLogViewSource(
            ObservableGroupedOrderedCollection<TimeEntryViewModel> collection,
            string cellIdentifier,
            ITimeService timeService,
            ISchedulerProvider schedulerProvider)
            : base(collection, cellIdentifier)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.timeService = timeService;
            this.schedulerProvider = schedulerProvider;

            if (!UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
            {
                deleteTableViewRowAction = UITableViewRowAction.Create(
                    UITableViewRowActionStyle.Destructive,
                    Resources.Delete,
                    handleDeleteTableViewRowAction);
                deleteTableViewRowAction.BackgroundColor = Color.TimeEntriesLog.DeleteSwipeActionBackground.ToNativeColor();
            }

            FirstCell = firstCellSubject.AsDriver(schedulerProvider);
        }

        public override UIView GetViewForFooter(UITableView tableView, nint section)
            => new UIView(new CGRect(0, 0, UIScreen.MainScreen.Bounds.Width, SpaceBetweenSections));

        public override nfloat GetHeightForHeader(UITableView tableView, nint section) => headerHeight;

        public override nfloat GetHeightForFooter(UITableView tableView, nint section) => SpaceBetweenSections;

        public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) => rowHeight;

        // It needs this method, otherwise the ContentOffset will reset to 0 everytime the table reloads. ¯\_(ツ)_/¯
        public override nfloat EstimatedHeight(UITableView tableView, NSIndexPath indexPath) => rowHeight;

        public override UITableViewRowAction[] EditActionsForRow(UITableView tableView, NSIndexPath indexPath)
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                return new[] { deleteTableViewRowAction };

            return new UITableViewRowAction[]{};
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = (TimeEntriesLogViewCell)base.GetCell(tableView, indexPath);

            cell.ContinueButtonTap
                .Subscribe(() => continueTapSubject.OnNext(cell.Item))
                .DisposedBy(cell.DisposeBag);

            if (indexPath.Row == 0 && indexPath.Section == 0)
                firstCellSubject.OnNext(cell);

            return cell;
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            var header = (TimeEntriesLogHeaderView)tableView.DequeueReusableHeaderFooterView(TimeEntriesLogHeaderView.Identifier);
            header.Items = DisplayedItems[(int)section];
            header.Now = timeService.CurrentDateTime;
            return header;
        }

        public override void RefreshHeader(UITableView tableView, int section)
        {
            if (tableView.GetHeaderView(section) is TimeEntriesLogHeaderView header)
            {
                header.Items = DisplayedItems[section];
                header.Now = timeService.CurrentDateTime;
            }
        }

        private void handleDeleteTableViewRowAction(UITableViewRowAction rowAction, NSIndexPath indexPath)
        {
            var timeEntry = DisplayedItems[indexPath.Section][indexPath.Row];
            swipeToDeleteSubject.OnNext(timeEntry);
        }

        public override UISwipeActionsConfiguration GetLeadingSwipeActionsConfiguration(UITableView tableView, NSIndexPath indexPath)
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                return null;

            var item = DisplayedItems[indexPath.Section][indexPath.Row];
            if (item == null)
                return null;

            return UISwipeActionsConfiguration
                .FromActions(new[] { continueSwipeActionFor(item) });
        }

        public override UISwipeActionsConfiguration GetTrailingSwipeActionsConfiguration(UITableView tableView, NSIndexPath indexPath)
        {
            if (!UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                return null;

            var item = DisplayedItems[indexPath.Section][indexPath.Row];
            if (item == null)
                return null;

            return UISwipeActionsConfiguration
                .FromActions(new[] { deleteSwipeActionFor(item) });
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            base.RowSelected(tableView, indexPath);
            tableView.DeselectRow(indexPath, true);
        }

        private UIContextualAction continueSwipeActionFor(TimeEntryViewModel timeEntry)
        {
            var continueAction = UIContextualAction.FromContextualActionStyle(
                UIContextualActionStyle.Normal,
                Resources.Continue,
                (action, sourceView, completionHandler) =>
                {
                    swipeToContinueSubject.OnNext(timeEntry);
                    completionHandler.Invoke(finished: true);
                }
            );
            continueAction.BackgroundColor = Color.TimeEntriesLog.ContinueSwipeActionBackground.ToNativeColor();
            return continueAction;
        }

        private UIContextualAction deleteSwipeActionFor(TimeEntryViewModel timeEntry)
        {
            var deleteAction = UIContextualAction.FromContextualActionStyle(
                UIContextualActionStyle.Destructive,
                Resources.Delete,
                (action, sourceView, completionHandler) =>
                {
                    swipeToDeleteSubject.OnNext(timeEntry);
                    completionHandler.Invoke(finished: true);
                }
            );
            deleteAction.BackgroundColor = Color.TimeEntriesLog.DeleteSwipeActionBackground.ToNativeColor();
            return deleteAction;
        }
    }
}
