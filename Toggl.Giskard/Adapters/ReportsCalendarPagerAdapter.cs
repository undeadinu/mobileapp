using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Content;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar;
using Toggl.Giskard.Views;
using Toggl.Multivac.Extensions;
using Object = Java.Lang.Object;

namespace Toggl.Giskard.Adapters
{
    public sealed class ReportsCalendarPagerAdapter : PagerAdapter
    {
        private static readonly int itemWidth;

        private CompositeDisposable disposeBag = new CompositeDisposable();
        private readonly Context context;
        private readonly RecyclerView.RecycledViewPool recyclerviewPool = new RecyclerView.RecycledViewPool();
        private IReadOnlyList<ReportsCalendarPageViewModel> currentMonths = ImmutableList<ReportsCalendarPageViewModel>.Empty;
        private Subject<ReportsCalendarDayViewModel> dayTaps = new Subject<ReportsCalendarDayViewModel>();
        private Subject<ReportsDateRangeParameter> selectionChanges = new Subject<ReportsDateRangeParameter>();
        private ReportsDateRangeParameter currentDateRange;

        public IObservable<ReportsCalendarDayViewModel> DayTaps => dayTaps.AsObservable();

        public ReportsCalendarPagerAdapter(Context context)
        {
            this.context = context;
        }

        public ReportsCalendarPagerAdapter(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override int Count => currentMonths.Count;

        public override Object InstantiateItem(ViewGroup container, int position)
        {
            var inflater = LayoutInflater.FromContext(context);
            var inflatedView = inflater.Inflate(Resource.Layout.ReportsCalendarFragmentPage, container, false);

            var calendarRecyclerView = (ReportsCalendarRecyclerView) inflatedView;
            calendarRecyclerView.SetRecycledViewPool(recyclerviewPool);
            calendarRecyclerView.SetLayoutManager(new ReportsCalendarLayoutManager(context));
            var adapter = new ReportsCalendarRecyclerAdapter(currentDateRange)
            {
                Items = currentMonths[position].Days
            };

            calendarRecyclerView.SetAdapter(adapter);

            adapter.ItemTapObservable
                .Subscribe(dayTaps.OnNext)
                .DisposedBy(disposeBag);

            selectionChanges
                .Subscribe(adapter.UpdateDateRangeParameter)
                .DisposedBy(disposeBag);

            container.AddView(inflatedView);

            return inflatedView;
        }

        public override void DestroyItem(ViewGroup container, int position, Object @object)
        {
            container.RemoveView(@object as View);
        }

        public override bool IsViewFromObject(View view, Object @object)
            => view == @object;

        public void UpdateMonths(List<ReportsCalendarPageViewModel> newMonths)
        {
            currentMonths = newMonths.ToImmutableList();
            NotifyDataSetChanged();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;

            disposeBag.Dispose();
        }

        public void UpdateSelectedRange(ReportsDateRangeParameter newDateRange)
        {
            currentDateRange = newDateRange;
            selectionChanges.OnNext(currentDateRange);
            NotifyDataSetChanged();
        }
    }
}
