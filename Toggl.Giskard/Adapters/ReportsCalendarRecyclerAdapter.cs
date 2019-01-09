using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using MvvmCross;
using MvvmCross.Platforms.Android;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.ViewHolders;

namespace Toggl.Giskard.Adapters
{
    public sealed class ReportsCalendarRecyclerAdapter : BaseRecyclerAdapter<ReportsCalendarDayViewModel>
    {
        private static readonly int itemWidth;
        private ReportsDateRangeParameter dateRangeParameter;
        public ReportsDateRangeParameter DateRangeParameter => dateRangeParameter;

        static ReportsCalendarRecyclerAdapter()
        {
            var context = Mvx.Resolve<IMvxAndroidGlobals>().ApplicationContext;
            var service = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            var display = service.DefaultDisplay;
            var size = new Point();
            display.GetSize(size);

            itemWidth = size.X / 7;
        }

        public ReportsCalendarRecyclerAdapter(ReportsDateRangeParameter dateRangeParameter)
        {
            this.dateRangeParameter = dateRangeParameter;
        }

        protected override BaseRecyclerViewHolder<ReportsCalendarDayViewModel> CreateViewHolder(ViewGroup parent, LayoutInflater inflater, int viewType)
        {
            var calendarDayCellViewHolder = new CalendarDayCellViewHolder(parent.Context);
            var layoutParams = new RecyclerView.LayoutParams(parent.LayoutParameters);
            layoutParams.Width = itemWidth;
            layoutParams.Height = 51.DpToPixels(parent.Context);
            calendarDayCellViewHolder.ItemView.LayoutParameters = layoutParams;
            return calendarDayCellViewHolder;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            base.OnBindViewHolder(holder, position);
            (holder as CalendarDayCellViewHolder)?.UpdateSelectionState(dateRangeParameter);
        }

        public void UpdateDateRangeParameter(ReportsDateRangeParameter newDateRange)
        {
            dateRangeParameter = newDateRange;
            NotifyDataSetChanged();
        }
    }
}
