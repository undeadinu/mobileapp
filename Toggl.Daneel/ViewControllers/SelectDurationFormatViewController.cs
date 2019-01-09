using System.Threading.Tasks;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Views;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public partial class SelectDurationFormatViewController
        : MvxViewController<SelectDurationFormatViewModel>,
          IDismissableViewController
    {
        public SelectDurationFormatViewController()
            : base(nameof(SelectDurationFormatViewController), null)
        {
        }

        public async Task<bool> Dismiss()
        {
            await ViewModel.CloseCommand.ExecuteAsync();
            return true;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TitleLabel.Text = Resources.DurationFormat;

            var source = new DurationFormatsTableViewSource(DurationFormatsTableView);
            DurationFormatsTableView.Source = source;

            var bindingSet = this.CreateBindingSet<SelectDurationFormatViewController, SelectDurationFormatViewModel>();

            bindingSet.Bind(source).To(vm => vm.DurationFormats);

            bindingSet.Bind(BackButton).To(vm => vm.CloseCommand);

            bindingSet.Bind(source)
                      .For(v => v.SelectionChangedCommand)
                      .To(vm => vm.SelectDurationFormatCommand);

            bindingSet.Apply();
        }
    }
}

