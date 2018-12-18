using System;
using System.Collections.Generic;
using System.Reactive;
using Toggl.Daneel.ViewSources;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;
using Toggl.Daneel.Extensions;
using System.Threading.Tasks;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Views.CountrySelection;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Multivac.Extensions;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class SelectCountryViewController : KeyboardAwareViewController<SelectCountryViewModel>, IDismissableViewController
    {
        private CountryTableViewSource tableViewSource = new CountryTableViewSource();

        public SelectCountryViewController() : base(nameof(SelectCountryViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            CountriesTableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            CountriesTableView.RegisterNibForCellReuse(CountryViewCell.Nib, CountryViewCell.Identifier);
            CountriesTableView.Source = tableViewSource;

            ViewModel.Countries
                .Subscribe(replaceCountries)
                .DisposedBy(DisposeBag);

            CloseButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            SearchTextField.Rx().Text()
                .Subscribe(ViewModel.SetFilterText.Inputs)
                .DisposedBy(DisposeBag);

            tableViewSource.CountrySelected
                .Subscribe(ViewModel.SelectCountry.Inputs)
                .DisposedBy(DisposeBag);

            SearchTextField.BecomeFirstResponder();
        }

        public async Task<bool> Dismiss()
        {
            ViewModel.Close.Execute(Unit.Default);
            return true;
        }

        private void replaceCountries(IEnumerable<SelectableCountryViewModel> countries)
        {
            tableViewSource.SetNewCountries(countries);
            CountriesTableView.ReloadData();
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.AnimateSetConstant(e.FrameEnd.Height, View);
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.AnimateSetConstant(0, View);
        }
    }
}

