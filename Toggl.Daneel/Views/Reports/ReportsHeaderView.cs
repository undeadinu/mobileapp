using System;
using CoreGraphics;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Platforms.Ios.Binding.Views;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Converters;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.MvvmCross.Combiners;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using UIKit;
using System.Reactive.Disposables;
using Toggl.Daneel.Extensions.Reactive;
using System.Reactive.Linq;
using Toggl.Multivac.Extensions;
using System.Linq;
using Toggl.Multivac;
using System.Collections.Generic;
using Toggl.Foundation.Conversions;
using System.Reactive.Subjects;
using System.Reactive;
using Toggl.Foundation.Extensions;

namespace Toggl.Daneel.Views.Reports
{
    public partial class ReportsHeaderView : MvxTableViewHeaderFooterView
    {
        private const float barChartSpacingProportion = 0.3f;

        public static readonly NSString Key = new NSString(nameof(ReportsHeaderView));
        public static readonly UINib Nib;

        public ReportsViewModel ViewModel { get; set; }

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();
        private readonly ISubject<Unit> updateLayout = new BehaviorSubject<Unit>(Unit.Default);

        static ReportsHeaderView()
        {
            Nib = UINib.FromName(nameof(ReportsHeaderView), NSBundle.MainBundle);
        }

        public ReportsHeaderView(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            var templateImage = TotalDurationGraph.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            TotalDurationGraph.Image = templateImage;

            prepareViews();

            this.DelayBind(() =>
            {
                //Text
                var reportPercentageConverter = new ReportPercentageLabelValueConverter();
                ViewModel.BillablePercentageObservable
                    .Select(reportPercentageConverter.Convert)
                    .Subscribe(BillablePercentageLabel.Rx().AttributedText())
                    .DisposedBy(disposeBag);

                ViewModel.TotalTimeObservable
                    .CombineLatest(ViewModel.DurationFormatObservable,
                        (totalTime, durationFormat) => totalTime.ToFormattedString(durationFormat))
                    .Subscribe(TotalDurationLabel.Rx().Text())
                    .DisposedBy(disposeBag);

                //Loading chart
                ViewModel.IsLoadingObservable
                    .Subscribe(LoadingPieChartView.Rx().IsVisibleWithFade())
                    .DisposedBy(disposeBag);

                ViewModel.IsLoadingObservable
                    .Subscribe(LoadingOverviewView.Rx().IsVisibleWithFade())
                    .DisposedBy(disposeBag);

                //Pretty stuff
                ViewModel.GroupedSegmentsObservable
                    .Subscribe(groupedSegments => PieChartView.Segments = groupedSegments)
                    .DisposedBy(disposeBag);

                ViewModel.BillablePercentageObservable
                    .Subscribe(percentage => BillablePercentageView.Percentage = percentage)
                    .DisposedBy(disposeBag);

                var totalDurationColorObservable = ViewModel.TotalTimeIsZeroObservable
                    .Select(isZero => isZero
                        ? Color.Reports.Disabled.ToNativeColor()
                        : Color.Reports.TotalTimeActivated.ToNativeColor());

                totalDurationColorObservable
                    .Subscribe(TotalDurationGraph.Rx().TintColor())
                    .DisposedBy(disposeBag);

                totalDurationColorObservable
                    .Subscribe(TotalDurationLabel.Rx().TextColor())
                    .DisposedBy(disposeBag);
                // Bar chart

                if (ViewModel == null)
                {
                    throw new InvalidOperationException($"The {nameof(ViewModel)} value must be set for {nameof(ReportsHeaderView)} before defining bindings.");
                }

                ViewModel.WorkspaceHasBillableFeatureEnabled
                    .Subscribe(ColorsLegendContainerView.Rx().IsVisible())
                    .DisposedBy(disposeBag);

                ViewModel.StartDate
                    .CombineLatest(
                        ViewModel.BarChartViewModel.DateFormat,
                        (startDate, format) => startDate.ToString(format.Short))
                    .Subscribe(StartDateLabel.Rx().Text())
                    .DisposedBy(disposeBag);

                ViewModel.EndDate
                    .CombineLatest(
                        ViewModel.BarChartViewModel.DateFormat,
                        (endDate, format) => endDate.ToString(format.Short))
                    .Subscribe(EndDateLabel.Rx().Text())
                    .DisposedBy(disposeBag);

                ViewModel.BarChartViewModel.MaximumHoursPerBar
                    .Select(hours => $"{hours} h")
                    .Subscribe(MaximumHoursLabel.Rx().Text())
                    .DisposedBy(disposeBag);

                ViewModel.BarChartViewModel.MaximumHoursPerBar
                    .Select(hours => $"{hours / 2} h")
                    .Subscribe(HalfHoursLabel.Rx().Text())
                    .DisposedBy(disposeBag);

                ViewModel.BarChartViewModel.HorizontalLegend
                    .Where(legend => legend == null)
                    .Subscribe((DateTimeOffset[] _) =>
                    {
                        HorizontalLegendStackView.Subviews.ForEach(subview => subview.RemoveFromSuperview());
                        StartDateLabel.Hidden = false;
                        EndDateLabel.Hidden = false;
                    })
                    .DisposedBy(disposeBag);

                ViewModel.BarChartViewModel.HorizontalLegend
                    .Where(legend => legend != null)
                    .CombineLatest(ViewModel.BarChartViewModel.DateFormat, createHorizontalLegendLabels)
                    .Do(_ =>
                    {
                        StartDateLabel.Hidden = true;
                        EndDateLabel.Hidden = true;
                    })
                    .Subscribe(HorizontalLegendStackView.Rx().ArrangedViews())
                    .DisposedBy(disposeBag);

                ViewModel.BarChartViewModel.Bars
                    .Select(createBarViews)
                    .Subscribe(BarsStackView.Rx().ArrangedViews())
                    .DisposedBy(disposeBag);

                var spacingObservable = ViewModel.BarChartViewModel.Bars
                    .CombineLatest(updateLayout, (bars, _) => bars)
                    .Select(bars => BarsStackView.Frame.Width / bars.Length * barChartSpacingProportion);

                spacingObservable
                    .Subscribe(BarsStackView.Rx().Spacing())
                    .DisposedBy(disposeBag);

                spacingObservable
                    .Subscribe(HorizontalLegendStackView.Rx().Spacing())
                    .DisposedBy(disposeBag);

                ViewModel.IsLoadingObservable
                    .Select(CommonFunctions.Invert)
                    .Subscribe(BarChartContainerView.Rx().IsVisible())
                    .DisposedBy(disposeBag);

                //Visibility
                ViewModel.ShowEmptyStateObservable
                    .Subscribe(EmptyStateView.Rx().IsVisible())
                    .DisposedBy(disposeBag);
            });
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            updateLayout.OnNext(Unit.Default);
        }

        private void prepareViews()
        {
            prepareCard(OverviewCardView);
            prepareCard(BarChartCardView);

            TotalTitleLabel.SetKerning(-0.2);
            TotalDurationLabel.SetKerning(-0.2);
            BillableTitleLabel.SetKerning(-0.2);
            BillablePercentageLabel.SetKerning(-0.2);
            ClockedHoursTitleLabel.SetKerning(-0.2);
            BillableLegendLabel.SetKerning(-0.2);
            NonBillableLegendLabel.SetKerning(-0.2);
        }

        private void prepareCard(UIView view)
        {
            view.Layer.CornerRadius = 8;
            view.Layer.ShadowColor = UIColor.Black.CGColor;
            view.Layer.ShadowRadius = 16;
            view.Layer.ShadowOffset = new CGSize(0, 2);
            view.Layer.ShadowOpacity = 0.1f;
        }

        private IEnumerable<UIView> createBarViews(IEnumerable<BarViewModel> bars)
            => bars.Select<BarViewModel, UIView>(bar =>
            {
                if (bar.NonBillablePercent == 0 && bar.BillablePercent == 0)
                {
                    return new EmptyBarView();
                }

                return new BarView(bar);
            });

        private IEnumerable<UILabel> createHorizontalLegendLabels(IEnumerable<DateTimeOffset> dates, DateFormat format)
            => dates.Select(date => new BarLegendLabel(DateTimeOffsetConversion.ToDayOfWeekInitial(date), date.ToString(format.Short)));
    }
}
