using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using System.Threading.Tasks;
using Toggl.Foundation;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class SelectTagsViewController : KeyboardAwareViewController<SelectTagsViewModel>, IDismissableViewController
    {
        public SelectTagsViewController() 
            : base(nameof(SelectTagsViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TitleLabel.Text = Resources.Tags;
            EmptyStateLabel.Text = Resources.EmptyTagText;
            CloseButton.SetTitle(Resources.Save, UIControlState.Normal);

            var source = new SelectTagsTableViewSource(TagsTableView);
            TagsTableView.Source = source;
            TagsTableView.TableFooterView = new UIView();

            var bindingSet = this.CreateBindingSet<SelectTagsViewController, SelectTagsViewModel>();

            bindingSet.Bind(EmptyStateImage)
                      .For(v => v.BindVisible())
                      .To(vm => vm.IsEmpty);
                
            bindingSet.Bind(EmptyStateLabel)
                      .For(v => v.BindVisible())
                      .To(vm => vm.IsEmpty);

            //Table view
            bindingSet.Bind(source).To(vm => vm.Tags);

            bindingSet.Bind(source)
                      .For(v => v.CurrentQuery)
                      .To(vm => vm.Text);

            bindingSet.Bind(source)
                      .For(v => v.CreateTagCommand)
                      .To(vm => vm.CreateTagCommand);

            bindingSet.Bind(source)
                      .For(v => v.SuggestCreation)
                      .To(vm => vm.SuggestCreation);
                           
            //Text
            bindingSet.Bind(TextField).To(vm => vm.Text);

            bindingSet.Bind(TextField)
                      .For(v => v.BindPlaceholder())
                      .To(vm => vm.PlaceholderText);

            //Commands
            bindingSet.Bind(CloseButton).To(vm => vm.CloseCommand);
            bindingSet.Bind(SaveButton).To(vm => vm.SaveCommand);
            bindingSet.Bind(source)
                      .For(v => v.SelectionChangedCommand)
                      .To(vm => vm.SelectTagCommand);

            bindingSet.Apply();

            TextField.BecomeFirstResponder();
        }

        public async Task<bool> Dismiss()
        {
            await ViewModel.CloseCommand.ExecuteAsync();
            return true;
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.Constant = e.FrameEnd.Height;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.Constant = 0;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }
    }
}
