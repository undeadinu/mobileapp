using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac.Models;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectCountryViewModelTests
    {
        public abstract class SelectCountryViewModelTest : BaseViewModelTests<SelectCountryViewModel>
        {
            protected override SelectCountryViewModel CreateViewModel()
                => new SelectCountryViewModel(NavigationService, RxActionFactory);

            protected List<ICountry> GenerateCountriesList() =>
                Enumerable.Range(1, 10).Select(i =>
                {
                    var country = Substitute.For<ICountry>();
                    country.Id.Returns(i);
                    country.Name.Returns(i.ToString());
                    country.CountryCode.Returns(i.ToString());
                    return country;
                }).ToList();
        }

        public sealed class TheConstructor : SelectCountryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useNavigationService,
                bool useRxActionFactory)
            {
                var navigationService = useNavigationService ? NavigationService : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SelectCountryViewModel(navigationService, rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheInitializeMethod : SelectCountryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task AddsAllCountriesToTheListOfSuggestions()
            {
                var countries = GenerateCountriesList();

                ViewModel.Prepare(10);

                await ViewModel.Initialize();

                ViewModel.Countries.First().Count().Should().Equals(250);
            }

            [Theory, LogIfTooSlow]
            [InlineData(1)]
            [InlineData(150)]
            [InlineData(200)]
            public async Task SetsTheAppropriateCountryAsTheCurrentlySelectedOne(int id)
            {
                ViewModel.Prepare(id);

                await ViewModel.Initialize();

                ViewModel.Countries.First().Single(c => c.Selected).Country.Id.Should().Be(id);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotSetTheSelectedCountryIfPreparingWithNull()
            {
                ViewModel.Prepare(null);

                await ViewModel.Initialize();

                ViewModel.Countries.First().All(suggestion => !suggestion.Selected);
            }
        }

        public sealed class TheSelectCountryCommand : SelectCountryViewModelTest
        {
            public TheSelectCountryCommand()
            {
                ViewModel.Prepare(10);
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModelWithSelectedCountryCode()
            {
                var country = Substitute.For<ICountry>();
                country.Id.Returns(1);
                country.Name.Returns("Greece");
                country.CountryCode.Returns("GR");

                await ViewModel.Initialize();

                var selectableCountry = new SelectableCountryViewModel(country, true);

                ViewModel.SelectCountry.Execute(selectableCountry);

                TestScheduler.Start();
                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), country.Id);
            }
        }

        public sealed class TheTextProperty : SelectCountryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task FiltersTheSuggestionsWhenItChanges()
            {
                ViewModel.Prepare(10);
                await ViewModel.Initialize();

                ViewModel.FilterText.OnNext("Greece");

                ViewModel.Countries.First().Count().Should().Equals(1);
            }
        }
    }
}
