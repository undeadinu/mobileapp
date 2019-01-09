using CoreGraphics;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Views;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalDialogPresentation]
    public partial class SelectDateTimeViewController : MvxViewController<SelectDateTimeViewModel>
    {
        public SelectDateTimeViewController() : base(nameof(SelectDateTimeViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TitleLabel.Text = Resources.Startdate;
            CloseButton.SetTitle(Resources.Save, UIControlState.Normal);

            prepareDatePicker();

            var bindingSet = this.CreateBindingSet<SelectDateTimeViewController, SelectDateTimeViewModel>();

            //Dates
            DatePicker.MinimumDate = ViewModel.MinDate.ToNSDate();
            DatePicker.MaximumDate = ViewModel.MaxDate.ToNSDate();

            bindingSet.Bind(DatePicker)
                      .For(v => v.BindDateTimeOffset())
                      .To(vm => vm.CurrentDateTime);

            //Commands
            bindingSet.Bind(CloseButton).To(vm => vm.CloseCommand);
            bindingSet.Bind(SaveButton).To(vm => vm.SaveCommand);
            
            bindingSet.Apply();
        }

        private void prepareDatePicker()
        {
            var screenWidth = UIScreen.MainScreen.Bounds.Width;
            PreferredContentSize = new CGSize
            {
                //ScreenWidth - 32 for 16pt margins on both sides
                Width = screenWidth > 320 ? screenWidth - 32 : 312,
                Height = View.Frame.Height
            };

            DatePicker.Locale = NSLocale.CurrentLocale;
            DatePicker.Mode = UIDatePickerMode.Date;
        }
    }
}
