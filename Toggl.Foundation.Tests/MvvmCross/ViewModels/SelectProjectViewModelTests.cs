using FluentAssertions;
using FsCheck.Xunit;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectProjectViewModelTests
    {
        public abstract class SelectProjectViewModelTest : BaseViewModelTests<SelectProjectViewModel>
        {
            protected override SelectProjectViewModel CreateViewModel()
            => new SelectProjectViewModel(DataSource, InteractorFactory, NavigationService, DialogService, SchedulerProvider, StopwatchProvider);
        }

        public sealed class TheConstructor : SelectProjectViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useInteractorFactory,
                bool useNavigationService,
                bool useDialogService,
                bool useSchedulerProvider,
                bool useStopwatchProvider)
            {
                var dataSource = useDataSource ? DataSource : null;
                var dialogService = useDialogService ? DialogService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var stopwatchProvider = useStopwatchProvider ? StopwatchProvider : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SelectProjectViewModel(dataSource, interactorFactory, navigationService, dialogService, schedulerProvider, stopwatchProvider);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheCloseCommand : SelectProjectViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<SelectProjectParameter>());
            }

            [Property]
            public void ReturnsTheSameProjectIdThatWasPassedToTheViewModel(long? projectId)
            {
                ViewModel.Prepare(SelectProjectParameter.WithIds(projectId, 10, 11));

                ViewModel.CloseCommand.ExecuteAsync().Wait();

                NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is<SelectProjectParameter>(
                        parameter => parameter.ProjectId == projectId)).Wait();
            }

            [Property]
            public void ReturnsTheSameTaskIdThatWasPassedToTheViewModel(long? taskId)
            {
                ViewModel.Prepare(SelectProjectParameter.WithIds(10, taskId, 11));

                ViewModel.CloseCommand.ExecuteAsync().Wait();

                NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is<SelectProjectParameter>(
                        parameter => parameter.TaskId == taskId)).Wait();
            }
        }

        public sealed class TheSelectProjectCommand : SelectProjectViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                ViewModel.SelectProjectCommand
                    .Execute(ProjectSuggestion.NoProject(0, ""));

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<SelectProjectParameter>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSelectedProjectIdWhenSelectingAProject()
            {
                var project = Substitute.For<IThreadSafeProject>();
                project.Id.Returns(13);
                var selectedProject = new ProjectSuggestion(project);

                ViewModel.SelectProjectCommand.Execute(selectedProject);

                await NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is<SelectProjectParameter>(
                        parameter => parameter.ProjectId == selectedProject.ProjectId));
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsNoTaskIdWhenSelectingAProject()
            {
                var project = Substitute.For<IThreadSafeProject>();
                project.Id.Returns(13);
                var selectedProject = new ProjectSuggestion(project);

                ViewModel.SelectProjectCommand.Execute(selectedProject);

                await NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is<SelectProjectParameter>(
                        parameter => parameter.TaskId == null));
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSelectedProjectIdWhenSelectingATask()
            {
                var task = Substitute.For<IThreadSafeTask>();
                task.Id.Returns(13);
                task.ProjectId.Returns(10);
                var selectedTask = new TaskSuggestion(task);

                ViewModel.SelectProjectCommand.Execute(selectedTask);

                await NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is<SelectProjectParameter>(
                        parameter => parameter.ProjectId == task.ProjectId));
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSelectedTaskIdWhenSelectingATask()
            {
                var task = Substitute.For<IThreadSafeTask>();
                task.Id.Returns(13);
                task.ProjectId.Returns(10);
                var selectedTask = new TaskSuggestion(task);

                ViewModel.SelectProjectCommand.Execute(selectedTask);

                await NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is<SelectProjectParameter>(
                        parameter => parameter.TaskId == task.Id));
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsNoProjectIfNoProjectWasSelected()
            {
                ViewModel.SelectProjectCommand
                    .Execute(ProjectSuggestion.NoProject(0, ""));

                await NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is<SelectProjectParameter>(
                        parameter => parameter.ProjectId == null));
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsNoTaskIfNoProjectWasSelected()
            {
                ViewModel.SelectProjectCommand
                    .Execute(ProjectSuggestion.NoProject(0, ""));

                await NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is<SelectProjectParameter>(
                        parameter => parameter.TaskId == null));
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsWorkspaceIfNoProjectWasSelected()
            {
                DialogService.Confirm(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>()
                ).Returns(Observable.Return(true));

                long workspaceId = 420;
                ViewModel.SelectProjectCommand
                    .Execute(ProjectSuggestion.NoProject(workspaceId, ""));

                await NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is<SelectProjectParameter>(
                        parameter => parameter.WorkspaceId == workspaceId));
            }

            [Fact, LogIfTooSlow]
            public void ShowsAlertIfWorkspaceIsGoingToBeChanged()
            {
                var oldWorkspaceId = 10;
                var newWorkspaceId = 11;
                ViewModel.Prepare(SelectProjectParameter.WithIds(null, null, oldWorkspaceId));
                var project = Substitute.For<IThreadSafeProject>();
                project.WorkspaceId.Returns(newWorkspaceId);

                ViewModel.SelectProjectCommand.Execute(new ProjectSuggestion(project));

                DialogService.Received().Confirm(
                    Arg.Is(Resources.DifferentWorkspaceAlertTitle),
                    Arg.Is(Resources.DifferentWorkspaceAlertMessage),
                    Arg.Is(Resources.Ok),
                    Arg.Is(Resources.Cancel)
                );
            }

            [Fact, LogIfTooSlow]
            public void DoesNotShowsAlertIfWorkspaceIsNotGoingToBeChanged()
            {
                var workspaceId = 10;
                ViewModel.Prepare(SelectProjectParameter.WithIds(null, null, workspaceId));
                var project = Substitute.For<IThreadSafeProject>();
                project.WorkspaceId.Returns(workspaceId);

                ViewModel.SelectProjectCommand.Execute(new ProjectSuggestion(project));

                DialogService.DidNotReceive().Confirm(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>()
                );
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsWorkspaceIdOfTheProjectIfProjectWasSelected()
            {
                var project = Substitute.For<IThreadSafeProject>();
                project.WorkspaceId.Returns(13);
                var projectSuggestion = new ProjectSuggestion(project);
                prepareDialogService();

                ViewModel.SelectProjectCommand.Execute(projectSuggestion);

                await ensureReturnsWorkspaceIdOfSuggestion(projectSuggestion);
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsWorksaceIdIfNoProjectWasSelected()
            {
                var noProjectSuggestion = ProjectSuggestion.NoProject(13, "");
                prepareDialogService();

                ViewModel.SelectProjectCommand.Execute(noProjectSuggestion);

                await ensureReturnsWorkspaceIdOfSuggestion(noProjectSuggestion);
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsWorkspaceIdOfTheTaskIfTaskWasSelected()
            {
                var task = Substitute.For<IThreadSafeTask>();
                task.Id.Returns(13);
                var taskSuggestion = new TaskSuggestion(task);
                prepareDialogService();

                ViewModel.SelectProjectCommand.Execute(taskSuggestion);

                await ensureReturnsWorkspaceIdOfSuggestion(taskSuggestion);
            }

            private void prepareDialogService()
                => DialogService.Confirm(
                       Resources.DifferentWorkspaceAlertTitle,
                       Resources.DifferentWorkspaceAlertMessage,
                       Resources.Ok,
                       Resources.Cancel).Returns(Observable.Return(true));

            private async Task ensureReturnsWorkspaceIdOfSuggestion(AutocompleteSuggestion suggestion)
            {
                await NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is<SelectProjectParameter>(
                        parameter => parameter.WorkspaceId == suggestion.WorkspaceId));
            }
        }

        public sealed class TheTextProperty : SelectProjectViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task WhenChangedUsesTheGetProjectsAutocompleteSuggestionsInteractor()
            {
                var text = "Some text";
                await ViewModel.Initialize();

                TestScheduler.Start();
                ViewModel.Text = text;
                TestScheduler.AdvanceBy(TimeSpan.FromSeconds(60).Ticks);

                InteractorFactory
                    .Received()
                    .GetProjectsAutocompleteSuggestions(Arg.Is<IList<string>>(
                        words => words.SequenceEqual(text.SplitToQueryWords())));
            }
        }

        public sealed class TheSuggestCreationProperty : SelectProjectViewModelTest
        {
            private const string name = "My project";

            public TheSuggestCreationProperty()
            {
                var project = Substitute.For<IThreadSafeProject>();
                project.Name.Returns(name);
                var suggestion = new ProjectSuggestion(project);

                InteractorFactory
                    .GetProjectsAutocompleteSuggestions(Arg.Any<IList<string>>())
                    .Execute()
                    .Returns(Observable.Return(new List<ProjectSuggestion> { suggestion }));

                ViewModel.Prepare();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheTextIsEmpty()
            {
                await ViewModel.Initialize();

                ViewModel.Text = "";

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheTextIsOnlyWhitespace()
            {
                await ViewModel.Initialize();

                ViewModel.Text = "       ";

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheTextIsLongerThanTwoHundredAndFiftyCharacters()
            {
                await ViewModel.Initialize();

                ViewModel.Text = "Some absurdly long project name created solely for making sure that the SuggestCreation property returns false when the project name is longer than the previously specified threshold so that the mobile apps behave and avoid crashes in backend and even bigger problems.";

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfNoWorkspaceIsEligible()
            {
                var workspace = new MockWorkspace { Id = 1, Name = "ws", Admin = false, OnlyAdminsMayCreateProjects = true };
                InteractorFactory.GetAllWorkspaces().Execute().Returns(Observable.Return(new[] { workspace } ));

                await ViewModel.Initialize();
                ViewModel.SuggestCreation.Should().BeFalse();
            }
        }

        public sealed class TheCreateProjectCommand : SelectProjectViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task DoesNotCloseTheViewModelIfTheProjectIsNotCreated()
            {
                setupProjectCreationResult(null);
                ViewModel.Prepare(SelectProjectParameter.WithIds(null, null, 10));
                await ViewModel.Initialize();
                ViewModel.Text = "New project name";

                await ViewModel.CreateProjectCommand.ExecuteAsync();

                await NavigationService.DidNotReceive().Close(ViewModel, Arg.Any<SelectProjectParameter>());
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModelReturningTheCreatedIdIfTheProjectIsCreated()
            {
                var workspace = new MockWorkspace { Id = 1, Name = "ws", Admin = true, OnlyAdminsMayCreateProjects = true };
                InteractorFactory.GetAllWorkspaces().Execute().Returns(Observable.Return(new[] { workspace } ));
                const long projectId = 10;
                setupProjectCreationResult(projectId);
                ViewModel.Prepare(SelectProjectParameter.WithIds(null, null, 10));
                await ViewModel.Initialize();
                ViewModel.Text = "New project name";

                await ViewModel.CreateProjectCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Close(ViewModel, Arg.Is<SelectProjectParameter>(p => p.ProjectId == projectId));
            }

            private void setupProjectCreationResult(long? returnedId)
            {
                NavigationService
                    .Navigate<EditProjectViewModel, string, long?>(Arg.Any<string>())
                    .Returns(Task.FromResult(returnedId));

                if (returnedId == null) return;

                var project = Substitute.For<IThreadSafeProject>();
                project.Id.Returns(returnedId.Value);
                InteractorFactory.GetProjectById(returnedId.Value).Execute().Returns(Observable.Return(project));
            }
        }

        public sealed class TheSuggestionsProperty : SelectProjectViewModelTest
        {
            private IEnumerable<ProjectSuggestion> getProjectSuggestions(int count, int workspaceId)
            {
                for (int i = 0; i < count; i++)
                    yield return getProjectSuggestion(i, workspaceId);
            }

            private ProjectSuggestion getProjectSuggestion(int projectId, int workspaceId)
            {
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                workspace.Name.Returns($"Workspace{workspaceId}");
                workspace.Id.Returns(workspaceId);
                var project = Substitute.For<IThreadSafeProject>();
                project.Name.Returns($"Project{projectId}");
                project.Workspace.Returns(workspace);
                project.Active.Returns(true);
                return new ProjectSuggestion(project);
            }

            [Fact, LogIfTooSlow]
            public async Task IsClearedWhenTextIsChanged()
            {
                var suggestions = getProjectSuggestions(1, 1);
                var queryText = "Query text";
                InteractorFactory
                    .GetProjectsAutocompleteSuggestions(Arg.Any<IList<string>>())
                    .Execute()
                    .Returns(Observable.Return(suggestions));
                await ViewModel.Initialize();
                ViewModel.Text = queryText;

                TestScheduler.Start();

                ViewModel.Suggestions.Should().HaveCount(1);
                ViewModel.Suggestions.First().Should().HaveCount(1);
            }

            [Fact, LogIfTooSlow]
            public async Task IsPopulatedAfterInitialization()
            {
                var projectSuggestions = getProjectSuggestions(10, 0);
                var suggestionsObservable = Observable.Return(projectSuggestions);
                InteractorFactory
                    .GetProjectsAutocompleteSuggestions(Arg.Any<IList<string>>())
                    .Execute()
                    .Returns(suggestionsObservable);

                await ViewModel.Initialize();
                TestScheduler.Start();

                ViewModel.Suggestions.Should().HaveCount(1);
                ViewModel.Suggestions.First().Should().HaveCount(11);
            }

            [Fact, LogIfTooSlow]
            public async Task PrependsEmptyProjectToEveryGroupIfFilterIsEmpty()
            {
                var suggestions = new List<ProjectSuggestion>();
                suggestions.AddRange(getProjectSuggestions(3, 0));
                suggestions.AddRange(getProjectSuggestions(4, 1));
                suggestions.AddRange(getProjectSuggestions(1, 10));
                suggestions.AddRange(getProjectSuggestions(10, 54));
                var suggestionsObservable = Observable.Return(suggestions);
                InteractorFactory
                    .GetProjectsAutocompleteSuggestions(Arg.Any<IList<string>>())
                    .Execute()
                    .Returns(suggestionsObservable);

                await ViewModel.Initialize();

                foreach (var group in ViewModel.Suggestions)
                {
                    group.Cast<ProjectSuggestion>().First().ProjectName.Should().Be(Resources.NoProject);
                }
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotPrependEmptyProjectToGroupsIfFilterIsUsed()
            {
                var suggestions = new List<ProjectSuggestion>();
                suggestions.AddRange(getProjectSuggestions(3, 0));
                suggestions.AddRange(getProjectSuggestions(4, 1));
                suggestions.AddRange(getProjectSuggestions(1, 10));
                suggestions.AddRange(getProjectSuggestions(10, 54));
                var suggestionsObservable = Observable.Return(suggestions);
                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                autocompleteProvider
                    .Query(Arg.Is<QueryInfo>(
                        info => info.SuggestionType == AutocompleteSuggestionType.Projects))
                    .Returns(suggestionsObservable);

                await ViewModel.Initialize();
                ViewModel.Text = suggestions.First().ProjectName;

                foreach (var group in ViewModel.Suggestions)
                {
                    group.Cast<ProjectSuggestion>().First().ProjectName.Should().NotBe(Resources.NoProject);
                }
            }

            [Fact, LogIfTooSlow]
            public async Task GroupsProjectsByWorkspace()
            {
                var suggestions = new List<ProjectSuggestion>();
                var workspaceIds = new[] { 0, 1, 10, 54 };
                suggestions.AddRange(getProjectSuggestions(3, workspaceIds[0]));
                suggestions.AddRange(getProjectSuggestions(4, workspaceIds[1]));
                suggestions.AddRange(getProjectSuggestions(1, workspaceIds[2]));
                suggestions.AddRange(getProjectSuggestions(10, workspaceIds[3]));
                var suggestionsObservable = Observable.Return(suggestions);
                InteractorFactory
                    .GetProjectsAutocompleteSuggestions(Arg.Any<IList<string>>())
                    .Execute()
                    .Returns(suggestionsObservable);

                await ViewModel.Initialize();
                TestScheduler.Start();

                ViewModel.Suggestions.Should().HaveCount(4);
                foreach (var suggestionGroup in ViewModel.Suggestions)
                {
                    foreach (var suggestion in suggestionGroup.Cast<ProjectSuggestion>())
                    {
                        if (suggestion.ProjectName == Resources.NoProject)
                            continue;
                        suggestion.WorkspaceName.Should().Be(suggestionGroup.WorkspaceName);
                        suggestion.WorkspaceId.Should().Be(suggestionGroup.WorkspaceId);
                    }
                }
            }

            [Fact, LogIfTooSlow]
            public async Task SortsProjectsByName()
            {
                var suggestions = new List<ProjectSuggestion>();
                suggestions.Add(getProjectSuggestion(3, 0));
                suggestions.Add(getProjectSuggestion(4, 1));
                suggestions.Add(getProjectSuggestion(1, 0));
                suggestions.Add(getProjectSuggestion(33, 1));
                suggestions.Add(getProjectSuggestion(10, 1));
                var suggestionsObservable = Observable.Return(suggestions);
                InteractorFactory
                    .GetProjectsAutocompleteSuggestions(Arg.Any<IList<string>>())
                    .Execute()
                    .Returns(suggestionsObservable);

                await ViewModel.Initialize();
                TestScheduler.Start();

                ViewModel.Suggestions.Should().HaveCount(2);
                foreach (var suggestionGroup in ViewModel.Suggestions)
                {
                    string prevProjectName = "";
                    foreach (var suggestion in suggestionGroup.Cast<ProjectSuggestion>())
                    {
                        if (suggestion.ProjectName == Resources.NoProject)
                            continue;
                        bool correctOrder = string.Compare(prevProjectName, suggestion.ProjectName, true) < 0;
                        correctOrder.Should().BeTrue();
                        prevProjectName = suggestion.ProjectName;
                    }
                }
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotContainSelectedProjectIfProjectIdIsNull()
            {
                prepareProjects();
                var parameter = SelectProjectParameter.WithIds(null, null, 0);
                ViewModel.Prepare(parameter);
                await ViewModel.Initialize();
                TestScheduler.Start();

                ViewModel.Suggestions.Should().HaveCount(1);
                ViewModel.Suggestions.First().Should().OnlyContain(
                    suggestion => ((ProjectSuggestion)suggestion).Selected == false);
            }

            [Fact, LogIfTooSlow]
            public async Task ContainsOnlyOneSelectedProjectIfProjectIdIsSet()
            {
                prepareProjects();
                long selectedProjectId = 5;
                var parameter = SelectProjectParameter.WithIds(selectedProjectId, null, 0);
                ViewModel.Prepare(parameter);
                await ViewModel.Initialize();
                TestScheduler.Start();

                ViewModel.Suggestions.Should().HaveCount(1);
                ViewModel.Suggestions.First().Should().OnlyContain(
                    suggestion => assertSuggestion(suggestion, selectedProjectId));
            }

            private bool assertSuggestion(AutocompleteSuggestion suggestion, long selectedProjectId)
            {
                var projectSuggestion = (ProjectSuggestion)suggestion;
                return projectSuggestion.Selected == false
                       || projectSuggestion.ProjectId == selectedProjectId && projectSuggestion.Selected;
            }

            private void prepareProjects()
            {
                var projects = Enumerable.Range(0, 30)
                    .Select(i =>
                    {
                        var project = Substitute.For<IThreadSafeProject>();
                        project.Id.Returns(i);
                        project.Workspace.Name.Returns("Ws");
                        return new ProjectSuggestion(project);
                    });

                InteractorFactory
                    .GetProjectsAutocompleteSuggestions(Arg.Any<IList<string>>())
                    .Execute()
                    .Returns(Observable.Return(projects));
            }
        }

        public sealed class TheIsEmptyProperty : SelectProjectViewModelTest
        {
            const long workspaceId = 1;

            private IThreadSafeProject createArbitraryProject(int id)
            {
                var project = Substitute.For<IThreadSafeProject>();
                project.Id.Returns(id);
                project.WorkspaceId.Returns(workspaceId);
                project.Name.Returns(Guid.NewGuid().ToString());
                return project;
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfHasProjects()
            {
                var projects = Enumerable.Range(0, 5)
                                         .Select(createArbitraryProject)
                                         .ToList();

                DataSource.Projects.GetAll().Returns(Observable.Return(projects));

                ViewModel.Prepare(SelectProjectParameter.WithIds(null, null, workspaceId));
                await ViewModel.Initialize();

                ViewModel.IsEmpty.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfHasProjectsButFilteredProjectCollectionDoesNot()
            {
                var projects = Enumerable.Range(0, 5)
                                         .Select(createArbitraryProject)
                                         .ToList();

                DataSource.Projects.GetAll().Returns(Observable.Return(projects));

                AutocompleteProvider
                          .Query(Arg.Is<QueryInfo>(arg => arg.SuggestionType == AutocompleteSuggestionType.Projects))
                          .Returns(Observable.Return(new List<ProjectSuggestion>()));

                ViewModel.Prepare(SelectProjectParameter.WithIds(null, null, workspaceId));
                await ViewModel.Initialize();

                ViewModel.Text = "Anything";

                ViewModel.IsEmpty.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTrueIfHasNoProjects()
            {
                DataSource.Projects.GetAll().Returns(Observable.Return(new List<IThreadSafeProject>()));

                ViewModel.Prepare(SelectProjectParameter.WithIds(null, null, workspaceId));
                await ViewModel.Initialize();

                ViewModel.IsEmpty.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsFalseBeforeLoadingProjectsFromDatabase()
            {
                DataSource.Projects.GetAll().Returns(Observable.Return(new List<IThreadSafeProject>()));

                ViewModel.Prepare(SelectProjectParameter.WithIds(null, null, workspaceId));

                ViewModel.IsEmpty.Should().BeFalse();
            }
        }
    }
}
