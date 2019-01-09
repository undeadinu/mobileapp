using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using Toggl.Foundation.Reports;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.ViewHelpers;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme",
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class ReportsActivity : ReactiveActivity<ReportsViewModel>
    {
        private static readonly TimeSpan toggleCalendarThrottleDuration = TimeSpan.FromMilliseconds(300);
        private ReportsRecyclerAdapter reportsRecyclerAdapter;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.ReportsActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_right, Resource.Animation.abc_fade_out);

            InitializeViews();
            setupToolbar();

            selectWorkspaceFAB.Rx().Tap()
                .Subscribe(ViewModel.SelectWorkspace.Inputs)
                .DisposedBy(DisposeBag);

            calendarView.SetupWith(ViewModel.CalendarViewModel);

            setupReportsRecyclerView();
            ViewModel.StartDate.CombineLatest(
                    ViewModel.EndDate,
                    ViewModel.WorkspaceHasBillableFeatureEnabled,
                    ViewModel.BarChartViewModel.DateFormat,
                    ViewModel.BarChartViewModel.Bars,
                    ViewModel.BarChartViewModel.MaximumHoursPerBar,
                    ViewModel.BarChartViewModel.HorizontalLegend,
                    BarChartData.Create)
                .Subscribe(reportsRecyclerAdapter.UpdateBarChart)
                .DisposedBy(DisposeBag);

            ViewModel.WorkspaceNameObservable
                .Subscribe(reportsRecyclerAdapter.UpdateWorkspaceName)
                .DisposedBy(DisposeBag);

            ViewModel.SegmentsObservable.CombineLatest(
                    ViewModel.ShowEmptyStateObservable,
                    ViewModel.TotalTimeObservable,
                    ViewModel.TotalTimeIsZeroObservable,
                    ViewModel.BillablePercentageObservable,
                    ReportsSummaryData.Create)
                .Subscribe(reportsRecyclerAdapter.UpdateReportsSummary)
                .DisposedBy(DisposeBag);

            reportsRecyclerAdapter.SummaryCardClicks
                .Subscribe(ViewModel.HideCalendar)
                .DisposedBy(DisposeBag);

            toolbarCurrentDateRangeText.Rx().Tap()
                .Throttle(toggleCalendarThrottleDuration)
                .Subscribe(ViewModel.ToggleCalendar)
                .DisposedBy(DisposeBag);

            ViewModel.CurrentDateRangeStringObservable
                .Subscribe(toolbarCurrentDateRangeText.Rx().TextObserver())
                .DisposedBy(DisposeBag);
        }

        public override void OnEnterAnimationComplete()
        {
            base.OnEnterAnimationComplete();
            ViewModel.StopNavigationFromMainLogStopwatch();
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_right);
        }

        private void setupReportsRecyclerView()
        {
            reportsRecyclerAdapter = new ReportsRecyclerAdapter(this);
            reportsRecyclerView.SetLayoutManager(new LinearLayoutManager(this));
            reportsRecyclerView.SetAdapter(reportsRecyclerAdapter);
        }

        private void setupToolbar()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.Toolbar);
            toolbar.Title = "";
            SetSupportActionBar(toolbar);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);

            toolbar.NavigationClick += onNavigateBack;
        }

        private void onNavigateBack(object sender, Toolbar.NavigationClickEventArgs e)
        {
            Finish();
        }

        internal void ToggleCalendarState(bool forceHide)
        {
            reportsMainContainer.ToggleCalendar(forceHide);
        }
    }
}
