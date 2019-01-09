using System;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts;

namespace Toggl.Giskard.ViewHolders
{
    public class ReportsCalendarShortcutCellViewHolder : BaseRecyclerViewHolder<ReportsCalendarBaseQuickSelectShortcut>
    {
        public static ReportsCalendarShortcutCellViewHolder Create(View itemView)
            => new ReportsCalendarShortcutCellViewHolder(itemView);

        private TextView shortcutText;
        private GradientDrawable backgroundDrawable;

        public ReportsCalendarShortcutCellViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public ReportsCalendarShortcutCellViewHolder(View itemView) : base(itemView)
        {
        }

        protected override void InitializeViews()
        {
            shortcutText = ItemView as TextView;
            backgroundDrawable = shortcutText?.Background as GradientDrawable;
        }

        protected override void UpdateView()
        {
            shortcutText.Text = Item.Title;
        }

        public void UpdateSelectionState(ReportsDateRangeParameter currentDateRange)
        {
            backgroundDrawable.SetColor(Item.IsSelected(currentDateRange) ? Color.ParseColor("#328fff") : Color.ParseColor("#3e3e3e"));
            backgroundDrawable.InvalidateSelf();
        }
    }
}
