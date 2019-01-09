using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CoreGraphics;
using Foundation;
using MvvmCross.Platforms.Ios.Binding.Views;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Views.Reports;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class ReportsTableViewSource : MvxTableViewSource
    {
        private const int summaryHeight = 469;
        private const string cellIdentifier = nameof(ReportsLegendViewCell);
        private const string headerCellIdentifier = nameof(ReportsHeaderView);

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();
        private readonly ReportsViewModel viewModel;

        public event EventHandler<CGPoint> OnScroll;

        private readonly nfloat headerHeight;
        private readonly nfloat bottomHeight = 78;

        public ReportsTableViewSource(UITableView tableView, ReportsViewModel viewModel)
            : base(tableView)
        {
            this.viewModel = viewModel;
            headerHeight = summaryHeight + UIScreen.MainScreen.Bounds.Width;
            tableView.TableHeaderView = new UIView(new CGRect(0, 0, tableView.Bounds.Size.Width, headerHeight));
            tableView.ContentInset = new UIEdgeInsets(-headerHeight, 0,  0, 0);
            tableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            tableView.RegisterNibForCellReuse(ReportsLegendViewCell.Nib, cellIdentifier);
            tableView.RegisterNibForHeaderFooterViewReuse(ReportsHeaderView.Nib, headerCellIdentifier);
            tableView.BackgroundColor = Color.Reports.Background.ToNativeColor();

            this.viewModel.WorkspacesObservable
                .Select(workspaces => workspaces.Count)
                .Subscribe(updateWorkspaceCount)
                .DisposedBy(disposeBag);
        }

        private void updateWorkspaceCount(int workspaceCount)
        {
            TableView.ContentInset = new UIEdgeInsets(-headerHeight, 0, workspaceCount > 1 ? bottomHeight : 0, 0);
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            var header = tableView.DequeueReusableHeaderFooterView(headerCellIdentifier);

            if (header is ReportsHeaderView reportsHeaderView)
                reportsHeaderView.ViewModel = viewModel;

            if (header is IMvxBindable bindable)
                bindable.DataContext = viewModel;

            return header;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var item = GetItemAt(indexPath);
            var cell = GetOrCreateCellFor(tableView, indexPath, item);
            cell.SelectionStyle = UITableViewCellSelectionStyle.None;

            if (item != null && cell is IMvxBindable bindable)
                bindable.DataContext = item;

            return cell;
        }

        protected override UITableViewCell GetOrCreateCellFor(UITableView tableView, NSIndexPath indexPath, object item)
            => tableView.DequeueReusableCell(cellIdentifier, indexPath);

        public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) => 56;

        public override nfloat GetHeightForHeader(UITableView tableView, nint section) => headerHeight;

        public override void Scrolled(UIScrollView scrollView)
        {
            var offset = scrollView.ContentOffset;
            OnScroll?.Invoke(this, new CGPoint(offset.X, offset.Y - headerHeight));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            disposeBag.Dispose();
        }
    }
}
