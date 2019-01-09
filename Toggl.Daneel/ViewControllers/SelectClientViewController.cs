using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using MvvmCross.Binding.BindingContext;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.Views.Client;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public partial class SelectClientViewController : KeyboardAwareViewController<SelectClientViewModel>, IDismissableViewController
    {
        private ClientTableViewSource tableViewSource = new ClientTableViewSource();

        public SelectClientViewController()
            : base(nameof(SelectClientViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TitleLabel.Text = Resources.Clients;
            SearchTextField.Placeholder = Resources.AddFilterClients;

            SuggestionsTableView.RegisterNibForCellReuse(ClientViewCell.Nib, ClientViewCell.Identifier);
            SuggestionsTableView.RegisterNibForCellReuse(CreateClientViewCell.Nib, CreateClientViewCell.Identifier);
            SuggestionsTableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            SuggestionsTableView.Source = tableViewSource;

            ViewModel.Clients
                .Subscribe(replaceClients)
                .DisposedBy(DisposeBag);

            CloseButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            SearchTextField.Rx().Text()
                .Subscribe(ViewModel.FilterText)
                .DisposedBy(DisposeBag);

            tableViewSource.ClientSelected
                .Subscribe(ViewModel.SelectClient.Inputs)
                .DisposedBy(DisposeBag);

            SearchTextField.BecomeFirstResponder();
        }

        public async Task<bool> Dismiss()
        {
            ViewModel.Close.Execute(Unit.Default);
            return true;
        }

        private void replaceClients(IEnumerable<SelectableClientBaseViewModel> clients)
        {
            tableViewSource.SetNewClients(clients);
            SuggestionsTableView.ReloadData();
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
