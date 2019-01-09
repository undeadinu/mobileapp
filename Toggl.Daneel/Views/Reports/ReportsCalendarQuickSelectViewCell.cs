using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts;
using UIKit;

namespace Toggl.Daneel.Views.Reports
{
    public sealed partial class ReportsCalendarQuickSelectViewCell : ReactiveCollectionViewCell<ReportsCalendarBaseQuickSelectShortcut>
    {
        public static readonly NSString Key = new NSString(nameof(ReportsCalendarQuickSelectViewCell));
        public static readonly UINib Nib;

        private ReportsDateRangeParameter currentDateRange;

        static ReportsCalendarQuickSelectViewCell()
        {
            Nib = UINib.FromName(nameof(ReportsCalendarQuickSelectViewCell), NSBundle.MainBundle);
        }

        public ReportsCalendarQuickSelectViewCell(IntPtr handle)
            : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            TitleLabel.Font = UIFont.SystemFontOfSize(13, UIFontWeight.Medium);
        }

        public void UpdateSelectedDateRange(ReportsDateRangeParameter dateRange)
        {
            currentDateRange = dateRange;
        }

        protected override void UpdateView()
        {
            TitleLabel.Text = Item.Title;

            ContentView.BackgroundColor = Item.IsSelected(currentDateRange)
                ? Color.ReportsCalendar.QuickSelect.SelectedBackground.ToNativeColor()
                : Color.ReportsCalendar.QuickSelect.UnselectedBackground.ToNativeColor();

            TitleLabel.TextColor = Item.IsSelected(currentDateRange)
                ? Color.ReportsCalendar.QuickSelect.SelectedTitle.ToNativeColor()
                : Color.ReportsCalendar.QuickSelect.UnselectedTitle.ToNativeColor();
        }
    }
}
