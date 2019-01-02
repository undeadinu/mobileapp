using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectClientViewModelTests
    {
        public abstract class SelectClientViewModelTest : BaseViewModelTests<SelectClientViewModel>
        {
            protected SelectClientParameters Parameters { get; }
                = SelectClientParameters.WithIds(10, null);

            protected override SelectClientViewModel CreateViewModel()
               => new SelectClientViewModel(InteractorFactory, NavigationService, SchedulerProvider, RxActionFactory);

            protected List<IThreadSafeClient> GenerateClientList() =>
                Enumerable.Range(1, 10).Select(i =>
                {
                    var client = Substitute.For<IThreadSafeClient>();
                    client.Id.Returns(i);
                    client.Name.Returns(i.ToString());
                    return client;
                }).ToList();
        }

        public sealed class TheConstructor : SelectClientViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useInteractorFactory,
                bool useNavigationService,
                bool useSchedulerProvider,
                bool useRxActionFactory)
            {
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SelectClientViewModel(interactorFactory, navigationService, schedulerProvider, rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheInitializeMethod : SelectClientViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task AddsAllClientsToTheListOfSuggestions()
            {
                var clients = GenerateClientList();
                InteractorFactory.GetAllClientsInWorkspace(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(clients));
                ViewModel.Prepare(Parameters);

                await ViewModel.Initialize();

                ViewModel.Clients.Count().Should().Equals(clients.Count);
            }

            [Fact, LogIfTooSlow]
            public async Task AddsANoClientSuggestion()
            {
                var clients = GenerateClientList();
                InteractorFactory.GetAllClientsInWorkspace(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(clients));
                ViewModel.Prepare(Parameters);

                await ViewModel.Initialize();

                ViewModel.Clients.First().First().Name.Should().Be(Resources.NoClient);
                ViewModel.Clients.First().First().Should().BeOfType<SelectableClientViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsNoClientAsSelectedIfTheParameterDoesNotSpecifyTheCurrentClient()
            {
                var clients = GenerateClientList();
                InteractorFactory.GetAllClientsInWorkspace(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(clients));
                ViewModel.Prepare(Parameters);

                await ViewModel.Initialize();

                ViewModel.Clients.First().Single(c => c.Selected).Name.Should().Be(Resources.NoClient);
            }

            [Theory, LogIfTooSlow]
            [InlineData(1)]
            [InlineData(2)]
            [InlineData(3)]
            [InlineData(4)]
            [InlineData(5)]
            [InlineData(6)]
            [InlineData(7)]
            [InlineData(8)]
            [InlineData(9)]
            public async Task SetsTheAppropriateClientAsTheCurrentlySelectedOne(int id)
            {
                var parameter = SelectClientParameters.WithIds(10, id);
                var clients = GenerateClientList();
                InteractorFactory.GetAllClientsInWorkspace(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(clients));
                ViewModel.Prepare(parameter);

                await ViewModel.Initialize();

                ViewModel.Clients.First().Single(c => c.Selected).Name.Should().Be(id.ToString());
            }
        }

        public sealed class TheCloseAction : SelectClientViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.Initialize();

                ViewModel.Close.Execute();
                TestScheduler.Start();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<long?>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsNull()
            {
                await ViewModel.Initialize();

                ViewModel.Close.Execute();
                TestScheduler.Start();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), null);
            }
        }

        public sealed class TheSelectClientAction : SelectClientViewModelTest
        {
            private readonly SelectableClientViewModel client = new SelectableClientViewModel(9, "Client A", false);

            public TheSelectClientAction()
            {
                var clients = GenerateClientList();
                InteractorFactory.GetAllClientsInWorkspace(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(clients));
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.Initialize();

                ViewModel.SelectClient.Execute(client);
                TestScheduler.Start();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<long?>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSelectedClientId()
            {
                await ViewModel.Initialize();

                ViewModel.SelectClient.Execute(client);
                TestScheduler.Start();

                await NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is<long?>(client.Id)
                );
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesANewClientWithTheGivenNameInTheCurrentWorkspace()
            {
                long workspaceId = 10;
                await ViewModel.Initialize();
                var newClient = new SelectableClientCreationViewModel("Some name of the client");
                ViewModel.Prepare(Parameters);

                ViewModel.SelectClient.Execute(newClient);
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .CreateClient(Arg.Is(newClient.Name), Arg.Is(workspaceId))
                    .Execute();
            }

            [Theory, LogIfTooSlow]
            [InlineData("   abcde", "abcde")]
            [InlineData("abcde     ", "abcde")]
            [InlineData("  abcde ", "abcde")]
            [InlineData("abcde  fgh", "abcde  fgh")]
            [InlineData("      abcd\nefgh     ", "abcd\nefgh")]
            public async Task TrimsNameFromTheStartAndTheEndBeforeSaving(string name, string trimmed)
            {
                await ViewModel.Initialize();

                ViewModel.SelectClient.Execute(new SelectableClientCreationViewModel(name));
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .CreateClient(Arg.Is(trimmed), Arg.Any<long>())
                    .Execute();
            }

        }

        public sealed class TheClientsProperty : SelectClientViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task UpdateWhenFilterTextChanges()
            {
                var clients = GenerateClientList();
                InteractorFactory.GetAllClientsInWorkspace(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(clients));
                await ViewModel.Initialize();

                ViewModel.FilterText.OnNext("0");

                ViewModel.Clients.Count().Should().Equals(1);
            }

            [Fact, LogIfTooSlow]
            public async Task AddCreationCellWhenNoMatchingSuggestion()
            {
                var clients = GenerateClientList();
                InteractorFactory.GetAllClientsInWorkspace(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(clients));
                await ViewModel.Initialize();

                var nonExistingClientName = "Some none existing name";
                ViewModel.FilterText.OnNext(nonExistingClientName);

                ViewModel.Clients.First().First().Name.Should().Equals(nonExistingClientName);
                ViewModel.Clients.First().First().Should().BeOfType<SelectableClientCreationViewModel>();
            }

            [Theory, LogIfTooSlow]
            [InlineData(" ")]
            [InlineData("\t")]
            [InlineData("\n")]
            [InlineData("               ")]
            [InlineData("      \t  \n     ")]
            [InlineData(null)]
            public async Task DoesNotSuggestCreatingClientsWhenTheDescriptionConsistsOfOnlyWhiteCharacters(string name)
            {
                var clients = GenerateClientList();
                InteractorFactory.GetAllClientsInWorkspace(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(clients));
                ViewModel.Prepare(Parameters);

                await ViewModel.Initialize();
                ViewModel.FilterText.OnNext(name);

                var receivedClients = await ViewModel.Clients.FirstAsync();
                receivedClients.First().Should().NotBeOfType<SelectableClientCreationViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotSuggestCreationWhenTextMatchesAExistingClientName()
            {
                var clients = GenerateClientList();
                InteractorFactory.GetAllClientsInWorkspace(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(clients));
                await ViewModel.Initialize();

                ViewModel.FilterText.OnNext(clients.First().Name);

                ViewModel.Clients.First().First().Should().NotBeOfType<SelectableClientCreationViewModel>();
            }
        }
    }
}
