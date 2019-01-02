using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Helper.Constants;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectClientViewModel : MvxViewModel<SelectClientParameters, long?>
    {
        private readonly IRxActionFactory rxActionFactory;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly ISchedulerProvider schedulerProvider;

        private long workspaceId;
        private long selectedClientId;
        private SelectableClientViewModel noClient;

        public IObservable<IEnumerable<SelectableClientBaseViewModel>> Clients { get; private set; }
        public ISubject<string> FilterText { get; } = new BehaviorSubject<string>(string.Empty);
        public UIAction Close { get; }
        public InputAction<SelectableClientBaseViewModel> SelectClient { get; }

        public SelectClientViewModel(
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            ISchedulerProvider schedulerProvider,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.interactorFactory = interactorFactory;
            this.navigationService = navigationService;
            this.rxActionFactory = rxActionFactory;

            Close = rxActionFactory.FromAsync(close);
            SelectClient = rxActionFactory.FromAsync<SelectableClientBaseViewModel>(selectClient);
        }

        public override void Prepare(SelectClientParameters parameter)
        {
            workspaceId = parameter.WorkspaceId;
            selectedClientId = parameter.SelectedClientId;
            noClient = new SelectableClientViewModel(0, Resources.NoClient, selectedClientId == 0);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            var allClients = await interactorFactory.GetAllClientsInWorkspace(workspaceId).Execute();

            Clients = FilterText
                .Select(text => text ?? string.Empty)
                .Select(text =>
                {
                    var trimmedText = text.Trim();
                    var selectableViewModels = allClients
                        .Where(c => c.Name.ContainsIgnoringCase(trimmedText))
                        .Select(toSelectableViewModel);

                    var isClientFilterEmpty = string.IsNullOrEmpty(trimmedText);
                    var suggestCreation = !isClientFilterEmpty
                                          && allClients.None(c => c.Name == trimmedText)
                                          && trimmedText.LengthInBytes() <= MaxClientNameLengthInBytes;

                    if (suggestCreation)
                    {
                        var creationSelectableViewModel =
                            new SelectableClientCreationViewModel(trimmedText);
                        selectableViewModels = selectableViewModels.Prepend(creationSelectableViewModel);
                    }
                    else if (isClientFilterEmpty)
                    {
                        selectableViewModels = selectableViewModels.Prepend(noClient);
                    }

                    return selectableViewModels;
                });
        }

        private SelectableClientBaseViewModel toSelectableViewModel(IThreadSafeClient client)
            => new SelectableClientViewModel(client.Id, client.Name, client.Id == selectedClientId);

        private Task close()
            => navigationService.Close(this, null);

        private async Task selectClient(SelectableClientBaseViewModel client)
        {
            switch (client)
            {
                case SelectableClientCreationViewModel c:
                    var newClient = await interactorFactory.CreateClient(c.Name.Trim(), workspaceId).Execute();
                    await navigationService.Close(this, newClient.Id);
                    break;
                case SelectableClientViewModel c:
                    await navigationService.Close(this, c.Id);
                    break;
            }
        }
    }
}
