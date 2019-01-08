using System;
using System.Threading.Tasks;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Platforms.Ios.Views;
using MvvmCross.Plugin.Color;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class EditProjectViewController : MvxViewController<EditProjectViewModel>, IDismissableViewController
    {
        private const float nameAlreadyTakenHeight = 16;

        public EditProjectViewController() 
            : base(nameof(EditProjectViewController), null)
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

            NameTakenErrorLabel.Text = Resources.NameTakenError;
            TitleLabel.Text = Resources.NewProject;
            NameTextField.Placeholder = Resources.ProjectName;
            DoneButton.SetTitle(Resources.Create, UIControlState.Normal);

            var heightConverter = new BoolToConstantValueConverter<nfloat>(nameAlreadyTakenHeight, 0);

            var bindingSet = this.CreateBindingSet<EditProjectViewController, EditProjectViewModel>();

            PrivateProjectSwitch.SetState(ViewModel.IsPrivate, false);

            //Commands
            bindingSet.Bind(DoneButton).To(vm => vm.DoneCommand);
            bindingSet.Bind(CloseButton).To(vm => vm.CloseCommand);
            bindingSet.Bind(ColorPickerOpeningView)
                      .For(v => v.BindTap())
                      .To(vm => vm.PickColorCommand);

            bindingSet.Bind(ClientLabel)
                      .For(v => v.BindTap())
                      .To(vm => vm.PickClientCommand);
            
            bindingSet.Bind(WorkspaceLabel)
                      .For(v => v.BindTap())
                      .To(vm => vm.PickWorkspaceCommand);

            bindingSet.Bind(ProjectNameUsedErrorTextHeight)
                      .For(v => v.Constant)
                      .To(vm => vm.IsNameAlreadyTaken)
                      .WithConversion(heightConverter);
            
            bindingSet.Bind(PrivateProjectSwitch)
                      .For(v => v.BindValueChanged())
                      .To(vm => vm.TogglePrivateProjectCommand);

            //State
            bindingSet.Bind(NameTextField).To(vm => vm.Name);
            bindingSet.Bind(WorkspaceLabel).To(vm => vm.WorkspaceName);

            bindingSet.Bind(ColorCircleView)
                      .For(v => v.BackgroundColor)
                      .To(vm => vm.Color)
                      .WithConversion(new MvxNativeColorValueConverter());

            bindingSet.Bind(DoneButton)
                      .For(v => v.Enabled)
                      .To(vm => vm.SaveEnabled);

            bindingSet.Bind(ClientLabel)
                      .For(v => v.AttributedText)
                      .To(vm => vm.ClientName)
                      .WithConversion(new AddNewAttributedStringConverter(Resources.AddClient, ClientLabel.Font.CapHeight));

            bindingSet.Apply();
        }
    }
}

