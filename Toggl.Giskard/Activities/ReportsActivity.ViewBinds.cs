using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Widget;
using Toggl.Giskard.Views;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Giskard.Activities
{
    public sealed partial class ReportsActivity
    {
        private FloatingActionButton selectWorkspaceFAB;
        private TextView workspaceName;
        private TextView toolbarCurrentDateRangeText;
        private RecyclerView reportsRecyclerView;
        private ReportsLinearLayout reportsMainContainer;
        private ReportsCalendarView calendarView;
        private Toolbar toolbar;

        protected override void InitializeViews()
        {
            selectWorkspaceFAB = FindViewById<FloatingActionButton>(Resource.Id.SelectWorkspaceFAB);
            workspaceName = FindViewById<TextView>(Resource.Id.ReportsActivityWorkspaceName);
            toolbarCurrentDateRangeText = FindViewById<TextView>(Resource.Id.ToolbarCurrentDateRangeText);
            reportsRecyclerView = FindViewById<RecyclerView>(Resource.Id.ReportsActivityRecyclerView);
            reportsMainContainer = FindViewById<ReportsLinearLayout>(Resource.Id.ReportsActivityMainContainer);
            calendarView = FindViewById<ReportsCalendarView>(Resource.Id.ReportsActivityCalendarView);
            reportsMainContainer.CalendarContainer = calendarView;
            toolbar = FindViewById<Toolbar>(Resource.Id.Toolbar);
        }
    }
}
