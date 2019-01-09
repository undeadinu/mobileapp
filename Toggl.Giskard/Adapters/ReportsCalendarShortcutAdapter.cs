using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts;
using Toggl.Giskard.ViewHolders;

namespace Toggl.Giskard.Adapters
{
    public class ReportsCalendarShortcutAdapter : BaseRecyclerAdapter<ReportsCalendarBaseQuickSelectShortcut>
    {
        private ReportsDateRangeParameter currentDateRange;

        protected override BaseRecyclerViewHolder<ReportsCalendarBaseQuickSelectShortcut> CreateViewHolder(ViewGroup parent, LayoutInflater inflater, int viewType)
        {
            var view = inflater.Inflate(Resource.Layout.ReportsCalendarShortcutCell, parent, false);
            return ReportsCalendarShortcutCellViewHolder.Create(view);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            base.OnBindViewHolder(holder, position);
            (holder as ReportsCalendarShortcutCellViewHolder)?.UpdateSelectionState(currentDateRange);
        }

        public void UpdateSelectedShortcut(ReportsDateRangeParameter newDateRange)
        {
            currentDateRange = newDateRange;
            NotifyDataSetChanged();
        }
    }
}
