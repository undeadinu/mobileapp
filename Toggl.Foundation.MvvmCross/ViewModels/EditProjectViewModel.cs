using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.UI;
using MvvmCross.ViewModels;
using PropertyChanged;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Helper.Constants;
using static Toggl.Multivac.Extensions.StringExtensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class EditProjectViewModel : MvxViewModel<string, long?>
    {
        private readonly Random random = new Random();
        private readonly ITogglDataSource dataSource;
        private readonly IDialogService dialogService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly IStopwatchProvider stopwatchProvider;

        private bool areCustomColorsEnabled;
        private long? clientId;
        private long workspaceId;
        private long initialWorkspaceId;
        private HashSet<string> projectNames = new HashSet<string>();
        private IStopwatch navigationFromStartTimeEntryViewModelStopwatch;

        public bool IsPrivate { get; set; }

        public bool IsNameAlreadyTaken { get; set; }

        [DependsOn(nameof(Name), nameof(IsNameAlreadyTaken))]
        public bool SaveEnabled =>
            !IsNameAlreadyTaken
            && !string.IsNullOrWhiteSpace(Name)
            && Name.LengthInBytes() <= MaxProjectNameLengthInBytes;

        public string Name { get; set; } = "";

        public string TrimmedName => Name.Trim();

        public MvxColor Color { get; set; }

        public string Title { get; private set; } = "";

        public string ClientName { get; private set; } = "";

        public string WorkspaceName { get; private set; } = "";

        public string DoneButtonText { get; private set; } = "";

        public IMvxAsyncCommand DoneCommand { get; }

        public IMvxAsyncCommand CloseCommand { get; }

        public IMvxAsyncCommand PickClientCommand { get; }

        public IMvxAsyncCommand PickColorCommand { get; }

        public IMvxAsyncCommand PickWorkspaceCommand { get; }

        public IMvxCommand TogglePrivateProjectCommand { get; }

        public EditProjectViewModel(
            ITogglDataSource dataSource,
            IDialogService dialogService,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IStopwatchProvider stopwatchProvider)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));

            this.dataSource = dataSource;
            this.dialogService = dialogService;
            this.navigationService = navigationService;
            this.stopwatchProvider = stopwatchProvider;
            this.interactorFactory = interactorFactory;

            DoneCommand = new MvxAsyncCommand(done);
            CloseCommand = new MvxAsyncCommand(close);
            PickColorCommand = new MvxAsyncCommand(pickColor);
            PickClientCommand = new MvxAsyncCommand(pickClient);
            PickWorkspaceCommand = new MvxAsyncCommand(pickWorkspace);
            TogglePrivateProjectCommand = new MvxCommand(togglePrivateProject);
        }

        public override void Prepare(string parameter)
        {
            Name = parameter;
            Title = Resources.NewProject;
            DoneButtonText = Resources.Create;
            pickRandomColor();
        }

        public override async Task Initialize()
        {
            navigationFromStartTimeEntryViewModelStopwatch = stopwatchProvider.Get(MeasuredOperation.OpenCreateProjectViewFromStartTimeEntryView);
            stopwatchProvider.Remove(MeasuredOperation.OpenCreateProjectViewFromStartTimeEntryView);

            var defaultWorkspace = await interactorFactory.GetDefaultWorkspace()
                .TrackException<InvalidOperationException, IThreadSafeWorkspace>("EditProjectViewModel.Initialize")
                .Execute();
            var allWorkspaces = await interactorFactory.GetAllWorkspaces().Execute();
            var workspace = defaultWorkspace.IsEligibleForProjectCreation()
                ? defaultWorkspace
                : allWorkspaces.First(ws => ws.IsEligibleForProjectCreation());

            areCustomColorsEnabled = await interactorFactory.AreCustomColorsEnabledForWorkspace(workspace.Id).Execute();
            workspaceId = initialWorkspaceId = workspace.Id;
            WorkspaceName = workspace.Name;

            await setupNameAlreadyTakenError();
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();
            navigationFromStartTimeEntryViewModelStopwatch?.Stop();
            navigationFromStartTimeEntryViewModelStopwatch = null;
        }

        private async Task setupNameAlreadyTakenError()
        {
            var existingProjectNames = await dataSource.Projects
                                        .GetAll(project => project.WorkspaceId == workspaceId)
                                        .Select(projects => projects.Select(p => p.Name));

            projectNames.Clear();
            projectNames.AddRange(existingProjectNames);

            IsNameAlreadyTaken = projectNames.Contains(TrimmedName);
        }

        private void OnNameChanged()
            => IsNameAlreadyTaken = projectNames.Contains(TrimmedName);

        private async Task pickColor()
        {
            Color = await navigationService.Navigate<SelectColorViewModel, ColorParameters, MvxColor>(
                ColorParameters.Create(Color, areCustomColorsEnabled));
        }

        private void togglePrivateProject()
        {
            IsPrivate = !IsPrivate;
        }

        private async Task done()
        {
            if (!SaveEnabled) return;

            if (initialWorkspaceId != workspaceId)
            {
                var shouldContinue = await dialogService.Confirm(
                    Resources.WorkspaceChangedAlertTitle,
                    Resources.WorkspaceChangedAlertMessage,
                    Resources.Ok,
                    Resources.Cancel
                );

                if (!shouldContinue) return;
            }

            var billable = await interactorFactory.AreProjectsBillableByDefault(workspaceId).Execute();

            var createdProject = await interactorFactory.CreateProject(new CreateProjectDTO
            {
                Name = TrimmedName,
                Color = Color.ToHexString(),
                IsPrivate = IsPrivate,
                ClientId = clientId,
                Billable = billable,
                WorkspaceId = workspaceId
            }).Execute();

            await navigationService.Close(this, createdProject.Id);
        }

        private Task close()
            => navigationService.Close(this, null);

        private async Task pickWorkspace()
        {
            var selectedWorkspaceId =
                await navigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(workspaceId);

            if (selectedWorkspaceId == workspaceId) return;

            var workspace = await interactorFactory.GetWorkspaceById(selectedWorkspaceId).Execute();

            clientId = null;
            ClientName = "";
            workspaceId = selectedWorkspaceId;
            WorkspaceName = workspace.Name;

            await setupNameAlreadyTakenError();

            areCustomColorsEnabled = await interactorFactory.AreCustomColorsEnabledForWorkspace(workspace.Id).Execute();
            if (areCustomColorsEnabled || Array.IndexOf(Helper.Color.DefaultProjectColors, Color) >= 0) return;

            pickRandomColor();
        }

        private async Task pickClient()
        {
            var parameter = SelectClientParameters.WithIds(workspaceId, clientId);
            var selectedClientId =
                await navigationService.Navigate<SelectClientViewModel, SelectClientParameters, long?>(parameter);
            if (selectedClientId == null) return;

            if (selectedClientId.Value == 0)
            {
                selectedClientId = null;
                ClientName = "";
                return;
            }

            var client = await interactorFactory.GetClientById(selectedClientId.Value).Execute();
            clientId = client.Id;
            ClientName = client.Name;
        }

        private void pickRandomColor()
        {
            var randomColorIndex = random.Next(0, Helper.Color.DefaultProjectColors.Length);
            Color = Helper.Color.DefaultProjectColors[randomColorIndex];
        }
    }
}
