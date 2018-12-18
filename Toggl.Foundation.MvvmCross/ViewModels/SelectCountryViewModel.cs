using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Services;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectCountryViewModel : MvxViewModel<long?, long?>
    {
        private readonly IRxActionFactory rxActionFactory;

        private long? selectedCountryId;

        private readonly ISubject<string> filterText = new BehaviorSubject<string>(string.Empty);

        private readonly IMvxNavigationService navigationService;

        public InputAction<SelectableCountryViewModel> SelectCountry { get; }

        public IObservable<IEnumerable<SelectableCountryViewModel>> Countries { get; private set; }

        public InputAction<string> SetFilterText { get; }
        public UIAction Close { get; }

        public SelectCountryViewModel(IMvxNavigationService navigationService, IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.navigationService = navigationService;
            this.rxActionFactory = rxActionFactory;

            SelectCountry = rxActionFactory.FromAsync<SelectableCountryViewModel>(selectCountry);
            SetFilterText = rxActionFactory.FromAction<string>(setText);
            Close = rxActionFactory.FromAsync(() => NavigationService.Close(this));
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            var allCountries = await new GetAllCountriesInteractor().Execute();

            var selectedElement = allCountries.Find(c => c.Id == selectedCountryId);
            if (selectedElement != null)
            {
                allCountries.Remove(selectedElement);
                allCountries.Insert(0, selectedElement);
            }

            Countries = filterText
                .Select(text => text.Trim())
                .DistinctUntilChanged()
                .Select(trimmedText =>
                {
                    return allCountries
                        .Where(c => c.Name.ContainsIgnoringCase(trimmedText))
                        .Select(c => new SelectableCountryViewModel(c, c.Id == selectedCountryId));
                });
        }

        public override void Prepare(long? parameter)
        {
            selectedCountryId = parameter;
        }

        private async Task selectCountry(SelectableCountryViewModel selectedCountry)
            => await navigationService.Close(this, selectedCountry.Country.Id);

        private void setText(string text)
        {
            filterText.OnNext(text ?? string.Empty);
        }
    }
}
