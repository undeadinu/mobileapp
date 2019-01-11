using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Span;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Exceptions;
using Toggl.PrimeRadiant.Models;
using Xunit;
using static Toggl.Foundation.Helper.Constants;
using static Toggl.Foundation.MvvmCross.Parameters.SelectTimeParameters.Origin;
using static Toggl.Multivac.Extensions.FunctionalExtensions;
using static Toggl.Multivac.Extensions.StringExtensions;
using ITimeEntryPrototype = Toggl.Foundation.Models.ITimeEntryPrototype;
using TextFieldInfo = Toggl.Foundation.Autocomplete.TextFieldInfo;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class StartTimeEntryViewModelTests
    {
        public abstract class StartTimeEntryViewModelTest : BaseViewModelTests<StartTimeEntryViewModel>
        {
            protected const string TagName = "Mobile";

            protected const long TagId = 20;
            protected const long TaskId = 30;
            protected const long ProjectId = 10;
            protected const long WorkspaceId = 40;
            protected const string WorkspaceName = "The best workspace ever";
            protected const string ProjectName = "Toggl";
            protected const string ProjectColor = "#F41F19";
            protected const string Description = "Testing Toggl mobile apps";

            protected ITestableObserver<TextFieldInfo> Observer { get; private set; }
            protected StartTimeEntryParameters DefaultParameter { get; } = new StartTimeEntryParameters(DateTimeOffset.UtcNow, "", null, null);

            protected override void AdditionalSetup()
            {
                DialogService.Confirm(
                   Arg.Any<string>(),
                   Arg.Any<string>(),
                   Arg.Any<string>(),
                   Arg.Any<string>()
               ).Returns(Observable.Return(true));
            }

            protected override void AdditionalViewModelSetup()
            {
                Observer = TestScheduler.CreateObserver<TextFieldInfo>();
                ViewModel.TextFieldInfoObservable.Subscribe(Observer);
            }

            protected override StartTimeEntryViewModel CreateViewModel()
                => new StartTimeEntryViewModel(
                    TimeService,
                    DataSource,
                    DialogService,
                    UserPreferences,
                    OnboardingStorage,
                    InteractorFactory,
                    NavigationService,
                    AnalyticsService,
                    AutocompleteProvider,
                    SchedulerProvider,
                    IntentDonationService,
                    StopwatchProvider
            );
        }

        public sealed class TheConstructor : StartTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useTimeService,
                bool useDialogService,
                bool useUserPreferences,
                bool useInteractorFactory,
                bool useOnboardingStorage,
                bool useNavigationService,
                bool useAnalyticsService,
                bool useAutocompleteProvider,
                bool useSchedulerProvider,
                bool useIntentDonationService,
                bool useStopwatchProvider)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var dialogService = useDialogService ? DialogService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var autocompleteProvider = useAutocompleteProvider ? AutocompleteProvider : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var intentDonationService = useIntentDonationService ? IntentDonationService : null;
                var stopwatchProvider = useStopwatchProvider ? StopwatchProvider : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new StartTimeEntryViewModel(
                        timeService,
                        dataSource,
                        dialogService,
                        userPreferences,
                        onboardingStorage,
                        interactorFactory,
                        navigationService,
                        analyticsService,
                        autocompleteProvider,
                        schedulerProvider,
                        intentDonationService,
                        stopwatchProvider);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class ThePrepareMethod : StartTimeEntryViewModelTest
        {
            [Property]
            public void SetsTheDateAccordingToTheDateParameterReceived(DateTimeOffset date, string placeholder, TimeSpan? duration)
            {
                var parameter = new StartTimeEntryParameters(date, placeholder, duration, null);

                ViewModel.Prepare(parameter);

                ViewModel.StartTime.Should().BeSameDateAs(date);
                ViewModel.PlaceholderText.Should().Be(placeholder);
                ViewModel.Duration.Should().Be(duration);
            }

            [Fact, LogIfTooSlow]
            public void StarsTheTimerWhenDurationIsNull()
            {
                var observable = Substitute.For<IConnectableObservable<DateTimeOffset>>();
                TimeService.CurrentDateTimeObservable.Returns(observable);
                var parameter = StartTimeEntryParameters.ForTimerMode(DateTimeOffset.Now);

                ViewModel.Prepare(parameter);

                TimeService.CurrentDateTimeObservable.ReceivedWithAnyArgs().Subscribe(null);
            }

            [Fact, LogIfTooSlow]
            public void SetsTheDisplayedTimeToTheValueOfTheDurationParameter()
            {
                var duration = TimeSpan.FromSeconds(130);
                var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", duration, null);

                ViewModel.Prepare(parameter);

                ViewModel.DisplayedTime.Should().Be(duration);
            }

            [Fact, LogIfTooSlow]
            public void ClearsTheIsDirtyFlag()
            {
                var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", null, null);

                ViewModel.Prepare(parameter);

                ViewModel.IsDirty.Should().BeFalse();
            }
        }

        public sealed class TheInitializeMethod : StartTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ChecksIfBillableIsAvailableForTheDefaultWorkspace(bool billableValue)
            {
                var workspace = new MockWorkspace { Id = 10 };
                InteractorFactory
                    .GetDefaultWorkspace()
                    .Execute()
                    .Returns(Observable.Return(workspace));
                InteractorFactory
                    .IsBillableAvailableForWorkspace(workspace.Id)
                    .Execute()
                    .Returns(Observable.Return(billableValue));
                var parameter = new StartTimeEntryParameters(DateTimeOffset.UtcNow, "", null, null);
                ViewModel.Prepare(parameter);

                await ViewModel.Initialize();

                ViewModel.IsBillableAvailable.Should().Be(billableValue);
            }
        }

        public abstract class TheSuggestCreationProperty : StartTimeEntryViewModelTest
        {
            protected abstract int MaxLength { get; }
            protected abstract char QuerySymbol { get; }
            protected abstract string QueryWithExactSuggestionMatch { get; }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfSuggestingTimeEntries()
            {
                await ViewModel.Initialize();

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("", 0));

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheCurrentQueryIsEmpty()
            {
                await ViewModel.Initialize();

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan($"{QuerySymbol}", 1));

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheCurrentQueryIsOnlyWhitespace()
            {
                await ViewModel.Initialize();

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan($"{QuerySymbol}    ", 1));

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheCurrentQueryIsLongerThanMaxLength()
            {
                await ViewModel.Initialize();

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan($"{QuerySymbol}{createLongString(MaxLength + 1)}", 1));

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfSuchSuggestionAlreadyExists()
            {
                await ViewModel.Initialize();

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan($"{QuerySymbol}{QueryWithExactSuggestionMatch}", 1));

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfWorkspaceSettingsDisableProjectCreation()
            {
                var workspace = new MockWorkspace { Id = 1, Admin = false, OnlyAdminsMayCreateProjects = true };
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(workspace));

                await ViewModel.Initialize();
                ViewModel.SuggestCreation.Should().BeFalse();
            }

            private string createLongString(int length)
                => Enumerable
                    .Range(0, length)
                    .Aggregate(new StringBuilder(), (builder, _) => builder.Append('A'))
                    .ToString();


            public sealed class WhenSuggestingProjects : TheSuggestCreationProperty
            {
                protected override int MaxLength => MaxProjectNameLengthInBytes;
                protected override char QuerySymbol => '@';
                protected override string QueryWithExactSuggestionMatch => ProjectName;

                public WhenSuggestingProjects()
                {
                    var project = Substitute.For<IThreadSafeProject>();
                    project.Id.Returns(10);
                    project.Name.Returns(ProjectName);
                    project.WorkspaceId.Returns(40);
                    project.Workspace.Name.Returns("Some workspace");
                    var projectSuggestion = new ProjectSuggestion(project);

                    AutocompleteProvider
                        .Query(Arg.Is<QueryInfo>(info => info.SuggestionType == AutocompleteSuggestionType.Projects))
                              .Returns(Observable.Return(new ProjectSuggestion[] { projectSuggestion }));

                    ViewModel.Prepare(DefaultParameter);
                }

                [Fact, LogIfTooSlow]
                public async Task ReturnsFalseIfAProjectIsAlreadySelected()
                {
                    var projectSpan = new ProjectSpan(ProjectId, ProjectName, ProjectColor, null, null);
                    var querySpan = new QueryTextSpan("abcde @fgh", 10);

                    await ViewModel.OnTextFieldInfoFromView(projectSpan);
                    await ViewModel.OnTextFieldInfoFromView(projectSpan, querySpan);

                    ViewModel.SuggestCreation.Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public async Task ReturnsFalseIfAProjectIsAlreadySelectedEvenIfInProjectSelectionMode()
                {
                    var projectSpan = new ProjectSpan(ProjectId, ProjectName, ProjectColor, null, null);
                    var querySpan = new QueryTextSpan("abcde @fgh", 10);

                    await ViewModel.OnTextFieldInfoFromView(projectSpan);
                    ViewModel.ToggleProjectSuggestionsCommand.Execute();

                    await ViewModel.OnTextFieldInfoFromView(projectSpan, querySpan);

                    ViewModel.SuggestCreation.Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public async Task TracksProjectSelection()
                {
                    ViewModel.Prepare();
                    TestScheduler.Start();

                    await ViewModel.Initialize();

                    await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("abcde @fgh", 10));

                    AnalyticsService.StartEntrySelectProject.Received().Track(ProjectTagSuggestionSource.TextField);
                }
            }

            public sealed class WhenSuggestingTags : TheSuggestCreationProperty
            {
                protected override int MaxLength => MaxTagNameLengthInBytes;
                protected override char QuerySymbol => '#';
                protected override string QueryWithExactSuggestionMatch => TagName;

                public WhenSuggestingTags()
                {
                    var tag = Substitute.For<IThreadSafeTag>();
                    tag.Id.Returns(20);
                    tag.Name.Returns(TagName);
                    var tagSuggestion = new TagSuggestion(tag);

                    AutocompleteProvider
                        .Query(Arg.Is<QueryInfo>(info => info.SuggestionType == AutocompleteSuggestionType.Tags))
                        .Returns(Observable.Return(new TagSuggestion[] { tagSuggestion }));

                    ViewModel.Prepare(DefaultParameter);
                }

                [Fact, LogIfTooSlow]
                public async Task ReturnsTrueNoMatterThatAProjectIsAlreadySelected()
                {
                    var projectSpan = new ProjectSpan(ProjectId, ProjectName, ProjectColor, null, null);
                    var querySpan = new QueryTextSpan("abcde #fgh", 10);

                    ViewModel.Prepare();
                    TestScheduler.Start();

                    await ViewModel.Initialize();
                    await ViewModel.OnTextFieldInfoFromView(projectSpan);

                    await ViewModel.OnTextFieldInfoFromView(projectSpan, querySpan);

                    ViewModel.SuggestCreation.Should().BeTrue();
                }

                [Fact, LogIfTooSlow]
                public async Task ReturnsTrueNoMatterThatAProjectIsAlreadySelectedAndInTagSuccestionMode()
                {
                    var projectSpan = new ProjectSpan(ProjectId, ProjectName, ProjectColor, null, null);
                    var querySpan = new QueryTextSpan("abcde #fgh", 10);

                    ViewModel.Prepare();
                    TestScheduler.Start();
                    await ViewModel.Initialize();
                    await ViewModel.OnTextFieldInfoFromView(projectSpan);
                    ViewModel.ToggleTagSuggestionsCommand.Execute();

                    await ViewModel.OnTextFieldInfoFromView(projectSpan, querySpan);

                    ViewModel.SuggestCreation.Should().BeTrue();
                }

                [Fact, LogIfTooSlow]
                public async Task ReturnsFalseIfAnOtherTagIsAvailableWithSameNameButDifferentCase()
                {
                    var projectSpan = new ProjectSpan(ProjectId, ProjectName, ProjectColor, null, null);
                    var querySpan = new QueryTextSpan("#mobile", 7);

                    ViewModel.Prepare();
                    await ViewModel.Initialize();
                    await ViewModel.OnTextFieldInfoFromView(projectSpan);
                    ViewModel.ToggleTagSuggestionsCommand.Execute();

                    await ViewModel.OnTextFieldInfoFromView(projectSpan, querySpan);
                    TestScheduler.Start();

                    ViewModel.SuggestCreation.Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public async Task TracksTagSelection()
                {
                    ViewModel.Prepare();
                    TestScheduler.Start();

                    await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("abcde #fgh", 10));

                    AnalyticsService.StartEntrySelectTag.Received().Track(ProjectTagSuggestionSource.TextField);
                }
            }
        }

        public sealed class TheCreateProjectCommand
        {
            public sealed class WhenSuggestingProjects : StartTimeEntryViewModelTest
            {
                private const string currentQuery = "My awesome Toggl project";

                public WhenSuggestingProjects()
                {
                    var project = Substitute.For<IThreadSafeProject>();
                    project.Id.Returns(10);
                    InteractorFactory.GetProjectById(Arg.Any<long>()).Execute().Returns(Observable.Return(project));
                    ViewModel.OnTextFieldInfoFromView(
                        new QueryTextSpan($"@{currentQuery}", 15)
                    ).GetAwaiter().GetResult();

                    ViewModel.Prepare();
                    ViewModel.Prepare(DefaultParameter);
                    TestScheduler.Start();
                }

                [Fact, LogIfTooSlow]
                public async Task CallsTheCreateProjectViewModel()
                {
                    await ViewModel.CreateCommand.ExecuteAsync();

                    await NavigationService.Received()
                        .Navigate<EditProjectViewModel, string, long?>(Arg.Any<string>());
                }

                [Fact, LogIfTooSlow]
                public async Task UsesTheCurrentQueryAsTheParameterForTheCreateProjectViewModel()
                {
                    await ViewModel.CreateCommand.ExecuteAsync();

                    await NavigationService.Received()
                        .Navigate<EditProjectViewModel, string, long?>(currentQuery);
                }

                [Fact, LogIfTooSlow]
                public async Task SelectsTheCreatedProject()
                {
                    long projectId = 200;
                    NavigationService
                        .Navigate<EditProjectViewModel, string, long?>(Arg.Is(currentQuery))
                        .Returns(projectId);
                    var project = Substitute.For<IThreadSafeProject>();
                    project.Id.Returns(projectId);
                    project.Name.Returns(currentQuery);
                    InteractorFactory.GetProjectById(Arg.Is(projectId)).Execute().Returns(Observable.Return(project));

                    await ViewModel.CreateCommand.ExecuteAsync();

                    var projectSpan = Observer.GetLatestInfo(TestScheduler).GetProjectSpan();
                    projectSpan.ProjectName.Should().Be(currentQuery);
                }
            }

            public sealed class WhenSuggestingTags : StartTimeEntryViewModelTest
            {
                private const string currentQuery = "My awesome Toggl project";

                private readonly QueryTextSpan querySpan = new QueryTextSpan($"#{currentQuery}", 1);

                public WhenSuggestingTags()
                {
                    ViewModel.Prepare();
                    ViewModel.Prepare(DefaultParameter);
                    TestScheduler.Start();
                    ViewModel.OnTextFieldInfoFromView(querySpan)
                        .GetAwaiter().GetResult();
                    TestScheduler.Start();
                }

                [Fact, LogIfTooSlow]
                public async Task CreatesTagWithCurrentQueryAsName()
                {
                    await ViewModel.CreateCommand.ExecuteAsync();

                    await InteractorFactory
                        .Received()
                        .CreateTag(Arg.Is(currentQuery), Arg.Any<long>())
                        .Execute();
                }

                [Fact, LogIfTooSlow]
                public async Task CreatesTagInProjectsWorkspaceIfAProjectIsSelected()
                {
                    long workspaceId = 100;
                    long projectId = 101;
                    var project = new MockProject
                    {
                        Id = projectId,
                        WorkspaceId = workspaceId
                    };
                    InteractorFactory.GetProjectById(Arg.Is(projectId)).Execute().Returns(Observable.Return(project));
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(new ProjectSuggestion(project));
                    await ViewModel.OnTextFieldInfoFromView(
                        querySpan, new ProjectSpan(projectId, "Project", "0000AF", null, null)
                    );

                    await ViewModel.CreateCommand.ExecuteAsync();

                    await InteractorFactory
                        .Received()
                        .CreateTag(Arg.Any<string>(), Arg.Is(workspaceId))
                        .Execute();
                }

                [Fact, LogIfTooSlow]
                public async Task CreatesTagInUsersDefaultWorkspaceIfNoProjectIsSelected()
                {
                    long workspaceId = 100;
                    var workspace = new MockWorkspace { Id = workspaceId };
                    InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(workspace));
                    var user = Substitute.For<IThreadSafeUser>();
                    user.DefaultWorkspaceId.Returns(workspaceId);
                    DataSource.User.Get().Returns(Observable.Return(user));
                    await ViewModel.Initialize();

                    await ViewModel.CreateCommand.ExecuteAsync();

                    await InteractorFactory
                        .Received()
                        .CreateTag(Arg.Any<string>(), Arg.Is(workspaceId))
                        .Execute();
                }

                [Fact, LogIfTooSlow]
                public async Task SelectsTheCreatedTag()
                {
                    InteractorFactory
                        .CreateTag(Arg.Any<string>(), Arg.Any<long>())
                        .Returns(callInfo =>
                        {
                            var tagName = callInfo.Arg<string>();
                            var tag = Substitute.For<IThreadSafeTag>();
                            tag.Name.Returns(tagName);
                            var interactor = Substitute.For<IInteractor<IObservable<IThreadSafeTag>>>();
                            interactor.Execute().Returns(Observable.Return(tag));
                            return interactor;
                        });

                    await ViewModel.CreateCommand.ExecuteAsync();

                    var tags = Observer.GetLatestInfo(TestScheduler).Spans.OfType<TagSpan>();
                    tags.Should().Contain(tag => tag.TagName == currentQuery);
                }
            }
        }

        public sealed class TheBackCommand : StartTimeEntryViewModelTest
        {
            public TheBackCommand()
            {
                var parameter = StartTimeEntryParameters.ForTimerMode(DateTimeOffset.Now);
                ViewModel.Prepare(parameter);
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModelIfUserDoesNotChangeAnything()
            {
                await ViewModel.BackCommand.ExecuteAsync();

                await NavigationService.Received().Close(ViewModel);
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsAConfirmationDialogIfUserEnteredSomething()
            {
                makeDirty();

                await ViewModel.BackCommand.ExecuteAsync();

                await DialogService.Received().ConfirmDestructiveAction(ActionType.DiscardNewTimeEntry);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotCloseTheViewIfUserWantsToContinueEditing()
            {
                makeDirty();
                DialogService.ConfirmDestructiveAction(ActionType.DiscardNewTimeEntry)
                             .Returns(_ => Observable.Return(false));

                await ViewModel.BackCommand.ExecuteAsync();

                await NavigationService.DidNotReceive().Close(ViewModel);
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewIfUserWantsToDiscardTheEnteredInformation()
            {
                makeDirty();
                DialogService.ConfirmDestructiveAction(ActionType.DiscardNewTimeEntry)
                             .Returns(_ => Observable.Return(true));

                await ViewModel.BackCommand.ExecuteAsync();

                await NavigationService.Received().Close(ViewModel);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotCallTheAnalyticsServiceSinceNoTimeEntryWasCreated()
            {
                await ViewModel.BackCommand.ExecuteAsync();

                AnalyticsService.DidNotReceive().Track(Arg.Any<StartTimeEntryEvent>());
            }

            private void makeDirty()
            {
                ViewModel.ToggleBillableCommand.Execute();
            }
        }

        public sealed class TheToggleBillableCommand : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void TogglesTheIsBillableProperty()
            {
                var expected = !ViewModel.IsBillable;

                ViewModel.ToggleBillableCommand.Execute();

                ViewModel.IsBillable.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void SetsTheIsDirtyFlag()
            {
                ViewModel.ToggleBillableCommand.Execute();

                ViewModel.IsDirty.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void TracksBillableTap()
            {
                ViewModel.ToggleBillableCommand.Execute();

                AnalyticsService.Received()
                                .StartViewTapped
                                .Track(StartViewTapSource.Billable);
            }
        }

        public sealed class TheToggleProjectSuggestionsCommand : StartTimeEntryViewModelTest
        {
            public TheToggleProjectSuggestionsCommand()
            {
                var suggestions = ProjectSuggestion.FromProjects(Enumerable.Empty<IThreadSafeProject>());
                AutocompleteProvider
                    .Query(Arg.Is<QueryInfo>(info => info.Text.Contains("@")))
                    .Returns(Observable.Return(suggestions));

                AutocompleteProvider
                    .Query(Arg.Is<QueryInfo>(
                        arg => arg.SuggestionType == AutocompleteSuggestionType.Projects))
                    .Returns(Observable.Return(suggestions));

                var defaultWorkspace = new MockWorkspace { Id = WorkspaceId };
                InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(defaultWorkspace));
            }

            private List<ProjectSuggestion> createProjects(int count)
                => Enumerable
                    .Range(0, count)
                    .Select(i =>
                    {
                        var workspace = Substitute.For<IThreadSafeWorkspace>();
                        workspace.Id.Returns(WorkspaceId);
                        workspace.Name.Returns(WorkspaceName);

                        var project = Substitute.For<IThreadSafeProject>();
                        project.Workspace.Returns(workspace);
                        project.WorkspaceId.Returns(WorkspaceId);
                        project.Id.Returns(ProjectId + i);
                        project.Name.Returns($"{ProjectName}-{i}");
                        project.Color.Returns(ProjectColor);
                        return project;
                    })
                    .Apply(ProjectSuggestion.FromProjects)
                    .ToList();

            [Fact, LogIfTooSlow]
            public async Task StartProjectSuggestionEvenIfTheProjectHasAlreadyBeenSelected()
            {
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.OnTextFieldInfoFromView(

                    new QueryTextSpan(Description, Description.Length),
                    new ProjectSpan(ProjectId, ProjectName, ProjectColor, null, null)
                );

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.IsSuggestingProjects.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void SetsTheIsSuggestingProjectsPropertyToTrueIfNotInProjectSuggestionMode()
            {
                ViewModel.Prepare();
                TestScheduler.Start();
                ViewModel.Prepare(DefaultParameter);

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.IsSuggestingProjects.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsCorrectProjectSuggestionsAfterProjectHasAlreadyBeenSelected()
            {
                const int noProjectCount = 1;
                var projects = createProjects(5);
                var chosenProject = projects.First();
                AutocompleteProvider.Query(Arg.Any<QueryInfo>()).Returns(Observable.Return(projects));
                ViewModel.Prepare();
                TestScheduler.Start();
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.Initialize();
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(Description, Description.Length));

                ViewModel.ToggleProjectSuggestionsCommand.Execute();
                await ViewModel.SelectSuggestionCommand.ExecuteAsync(chosenProject);
                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.Suggestions.Should().HaveCount(1);
                ViewModel.Suggestions.First().Should().HaveCount(projects.Count + noProjectCount);
            }

            [Fact, LogIfTooSlow]
            public async Task ProjectSuggestionsAreClearedOnProjectSelection()
            {
                var projects = createProjects(5);
                var chosenProject = projects.First();
                AutocompleteProvider.Query(Arg.Any<QueryInfo>()).Returns(Observable.Return(projects));
                ViewModel.Prepare();
                TestScheduler.Start();
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.Initialize();
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(Description, Description.Length));

                ViewModel.ToggleProjectSuggestionsCommand.Execute();
                await ViewModel.SelectSuggestionCommand.ExecuteAsync(chosenProject);

                ViewModel.IsSuggestingProjects.Should().BeFalse();
                ViewModel.Suggestions.Should().HaveCount(0);
            }

            [Fact, LogIfTooSlow]
            public async Task AddsAnAtSymbolAtTheEndOfTheQueryInOrderToStartProjectSuggestionMode()
            {
                const string description = "Testing Toggl Apps";
                var expected = $"{description} @";
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(description, description.Length));

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                var querySpan = Observer.GetLatestInfo(TestScheduler).GetQuerySpan();
                querySpan.Text.Should().Be(expected);
            }

            [Theory, LogIfTooSlow]
            [InlineData("@")]
            [InlineData("@somequery")]
            [InlineData("@some query")]
            [InlineData("@some query@query")]
            [InlineData("Testing Toggl Apps @")]
            [InlineData("Testing Toggl Apps @somequery")]
            [InlineData("Testing Toggl Apps @some query")]
            [InlineData("Testing Toggl Apps @some query @query")]
            public async Task SetsTheIsSuggestingProjectsPropertyToFalseIfAlreadyInProjectSuggestionMode(string description)
            {
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(description, description.Length));

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.IsSuggestingProjects.Should().BeFalse();
            }

            [Theory, LogIfTooSlow]
            [InlineData("@", "")]
            [InlineData("@somequery", "")]
            [InlineData("@some query", "")]
            [InlineData("@some query@query", "")]
            [InlineData("Testing Toggl Apps @", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps @somequery", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps @some query", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps @some query @query", "Testing Toggl Apps ")]
            public async Task RemovesTheAtSymbolFromTheDescriptionTextIfAlreadyInProjectSuggestionMode(
                string description, string expected)
            {
                ViewModel.Prepare();
                TestScheduler.Start();
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(description, description.Length));

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                var querySpan = Observer.GetLatestInfo(TestScheduler).GetQuerySpan();
                querySpan.Text.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotSetTheIsDirtyFlag()
            {
                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.IsDirty.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void SetsProjectOrTagWasAdded()
            {
                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                OnboardingStorage.Received().ProjectOrTagWasAdded();
            }

            [Fact, LogIfTooSlow]
            public void TracksShowProjectSuggestions()
            {
                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.Project);
                AnalyticsService.StartEntrySelectProject.Received().Track(ProjectTagSuggestionSource.ButtonOverKeyboard);
            }
        }

        public sealed class TheToggleTagSuggestionsCommand : StartTimeEntryViewModelTest
        {
            public TheToggleTagSuggestionsCommand()
            {
                var tag = Substitute.For<IThreadSafeTag>();
                tag.Id.Returns(TagId);
                tag.Name.Returns(TagName);
                var suggestions = TagSuggestion.FromTags(new[] { tag });
                AutocompleteProvider
                    .Query(Arg.Is<QueryInfo>(info => info.Text.Contains("#")))
                    .Returns(Observable.Return(suggestions));
            }

            [Fact, LogIfTooSlow]
            public void SetsTheIsSuggestingTagsPropertyToTrueIfNotInTagSuggestionMode()
            {
                ViewModel.Prepare();
                TestScheduler.Start();
                ViewModel.Prepare(DefaultParameter);
                ViewModel.ToggleTagSuggestionsCommand.Execute();

                ViewModel.IsSuggestingTags.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task AddsHashtagSymbolAtTheEndOfTheQueryInOrderToTagSuggestionMode()
            {
                const string description = "Testing Toggl Apps";
                var expected = $"{description} #";
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(description, description.Length));

                ViewModel.ToggleTagSuggestionsCommand.Execute();

                var querySpan = Observer.GetLatestInfo(TestScheduler).GetQuerySpan();
                querySpan.Text.Should().Be(expected);
            }

            [Theory, LogIfTooSlow]
            [InlineData("#")]
            [InlineData("#somequery")]
            [InlineData("#some query")]
            [InlineData("#some quer#query")]
            [InlineData("Testing Toggl Apps #")]
            [InlineData("Testing Toggl Apps #somequery")]
            [InlineData("Testing Toggl Apps #some query")]
            [InlineData("Testing Toggl Apps #some query #query")]
            public async Task SetsTheIsSuggestingTagsPropertyToFalseIfAlreadyInTagSuggestionMode(string description)
            {
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(description, description.Length));

                ViewModel.ToggleTagSuggestionsCommand.Execute();

                ViewModel.IsSuggestingTags.Should().BeFalse();
            }

            [Theory, LogIfTooSlow]
            [InlineData("#", "")]
            [InlineData("#somequery", "")]
            [InlineData("#some query", "")]
            [InlineData("#some query#query", "")]
            [InlineData("Testing Toggl Apps #", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps #somequery", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps #some query", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps #some query #query", "Testing Toggl Apps ")]
            public async Task RemovesTheHashtagSymbolFromTheDescriptionTextIfAlreadyInTagSuggestionMode(
                string description, string expected)
            {
                var viewModel = CreateViewModel();
                var observer = TestScheduler.CreateObserver<TextFieldInfo>();
                viewModel.TextFieldInfoObservable.Subscribe(observer);
                viewModel.Prepare();
                TestScheduler.Start();
                viewModel.Prepare(DefaultParameter);
                await viewModel.OnTextFieldInfoFromView(
                    new ProjectSpan(ProjectId, ProjectName, ProjectColor),
                    new QueryTextSpan(description, description.Length)
                );

                viewModel.ToggleTagSuggestionsCommand.Execute();

                var querySpan = observer.GetLatestInfo(TestScheduler).GetQuerySpan();
                querySpan.Text.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotSetTheIsDirtyFlag()
            {
                ViewModel.ToggleTagSuggestionsCommand.Execute();

                ViewModel.IsDirty.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void SetsProjectOrTagWasAdded()
            {
                ViewModel.ToggleTagSuggestionsCommand.Execute();

                OnboardingStorage.Received().ProjectOrTagWasAdded();
            }

            [Fact, LogIfTooSlow]
            public void TracksShowTagSuggestions()
            {
                ViewModel.ToggleTagSuggestionsCommand.Execute();

                AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.Tags);
                AnalyticsService.StartEntrySelectTag.Received().Track(ProjectTagSuggestionSource.ButtonOverKeyboard);
            }
        }

        public sealed class TheChangeTimeCommand : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task SetsTheStartDateToTheValueReturnedByTheEditDurationViewModel()
            {
                var now = DateTimeOffset.UtcNow;
                var parameter = new StartTimeEntryParameters(now, "", null, null);
                var parameterToReturn = DurationParameter.WithStartAndDuration(now.AddHours(-2), null);
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(Arg.Any<EditDurationParameters>())
                    .Returns(parameterToReturn);
                ViewModel.Prepare(parameter);

                await ViewModel.ChangeTimeCommand.ExecuteAsync();

                ViewModel.StartTime.Should().Be(parameterToReturn.Start);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheStopDateToTheValueReturnedByTheEditDurationViewModelIfUserStoppsTheTimeEntry()
            {
                var now = DateTimeOffset.UtcNow;
                var currentTimeSubject = new Subject<DateTimeOffset>();
                var parameter = new StartTimeEntryParameters(now, "", null, null);
                var observable = currentTimeSubject.AsObservable().Replay();
                observable.Connect();
                TimeService.CurrentDateTimeObservable.Returns(observable);
                var parameterToReturn = DurationParameter.WithStartAndDuration(now.AddHours(-2), TimeSpan.FromMinutes(30));
                var tcs = new TaskCompletionSource<DurationParameter>();
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(Arg.Any<EditDurationParameters>())
                    .Returns(tcs.Task);
                ViewModel.Prepare(parameter);

                var toWait = ViewModel.ChangeTimeCommand.ExecuteAsync();
                tcs.SetResult(parameterToReturn);
                await toWait;

                ViewModel.DisplayedTime.Should().Be(parameterToReturn.Duration.Value);

                currentTimeSubject.OnNext(now.AddHours(10));

                ViewModel.DisplayedTime.Should().Be(parameterToReturn.Duration.Value);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotSetTheIsDirtyFlagIfNothingChanges()
            {
                var now = DateTimeOffset.UtcNow;
                var parameter = new StartTimeEntryParameters(now, "", null,  null);
                ViewModel.Prepare(parameter);
                var parameterToReturn = DurationParameter.WithStartAndDuration(now, null);
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(Arg.Any<EditDurationParameters>())
                    .Returns(Task.FromResult(parameterToReturn));

                await ViewModel.ChangeTimeCommand.ExecuteAsync();

                ViewModel.IsDirty.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheIsDirtyFlagWhenStartTimeChanges()
            {
                var now = DateTimeOffset.UtcNow;
                var parameter = new StartTimeEntryParameters(now, "", null,  null);
                var parameterToReturn = DurationParameter.WithStartAndDuration(now.AddHours(-2), null);
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(Arg.Any<EditDurationParameters>())
                    .Returns(Task.FromResult(parameterToReturn));
                ViewModel.Prepare(parameter);

                await ViewModel.ChangeTimeCommand.ExecuteAsync();

                ViewModel.IsDirty.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheIsDirtyFlagWhenDurationChanges()
            {
                var now = DateTimeOffset.UtcNow;
                var parameter = new StartTimeEntryParameters(now, "", null,  null);
                var parameterToReturn = DurationParameter.WithStartAndDuration(now, TimeSpan.FromMinutes(10));
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(Arg.Any<EditDurationParameters>())
                    .Returns(Task.FromResult(parameterToReturn));
                ViewModel.Prepare(parameter);

                await ViewModel.ChangeTimeCommand.ExecuteAsync();

                ViewModel.IsDirty.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksStartTimeTap()
            {
                var now = DateTimeOffset.UtcNow;
                var parameters = StartTimeEntryParameters.ForTimerMode(now);
                var returnParameter = DurationParameter.WithStartAndDuration(now, TimeSpan.FromMinutes(1));
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(Arg.Any<EditDurationParameters>())
                    .Returns(Task.FromResult(returnParameter));
                ViewModel.Prepare(parameters);

                await ViewModel.ChangeTimeCommand.ExecuteAsync();

                AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.StartTime);
            }
        }

        public sealed class TheSetStartDateCommand : StartTimeEntryViewModelTest
        {
            private static readonly DateTimeOffset now = new DateTimeOffset(2018, 02, 13, 23, 59, 12, TimeSpan.FromHours(-1));
            private static readonly StartTimeEntryParameters prepareParameters = StartTimeEntryParameters.ForTimerMode(now);

            public TheSetStartDateCommand()
            {
                TimeService.CurrentDateTime.Returns(now);
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheSelectDateTimeViewModel()
            {
                ViewModel.Prepare(prepareParameters);

                await ViewModel.SetStartDateCommand.ExecuteAsync();

                await NavigationService.Received().Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(Arg.Any<DateTimePickerParameters>());
            }

            [Fact]
            public async Task OpensTheSelectDateTimeViewModelWithCorrectLimitsForARunningTimeEntry()
            {
                ViewModel.Prepare();
                ViewModel.Prepare(prepareParameters);

                await ViewModel.SetStartDateCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(
                        Arg.Is<DateTimePickerParameters>(param => param.MinDate == now - MaxTimeEntryDuration && param.MaxDate == now));
            }

            [Fact]
            public async Task OpensTheSelectDateTimeViewModelWithCorrectLimitsForAStoppedTimeEntry()
            {
                var stoppedParametsrs = StartTimeEntryParameters.ForManualMode(now);
                ViewModel.Prepare(stoppedParametsrs);

                await ViewModel.SetStartDateCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(
                        Arg.Is<DateTimePickerParameters>(param => param.MinDate == EarliestAllowedStartTime && param.MaxDate == LatestAllowedStartTime));
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheStartDateToTheValueReturnedByTheSelectDateTimeViewModel()
            {
                var parameterToReturn = now.AddDays(-2);
                NavigationService
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(Arg.Any<DateTimePickerParameters>())
                    .Returns(parameterToReturn);

                ViewModel.Prepare(prepareParameters);
                await ViewModel.SetStartDateCommand.ExecuteAsync();

                ViewModel.StartTime.Date.Should().Be(parameterToReturn.Date);
            }

            [Fact, LogIfTooSlow]
            public async Task RespectsTheTimeZone()
            {
                var currentTime = new DateTimeOffset(2018, 2, 20, 13, 20, 0, TimeSpan.FromHours(5));
                TimeService.CurrentDateTime.Returns(currentTime);
                var selectedDate = new DateTimeOffset(2018, 2, 14, 0, 0, 0, TimeSpan.FromHours(5));
                NavigationService
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(Arg.Any<DateTimePickerParameters>())
                    .Returns(selectedDate);

                ViewModel.Prepare(prepareParameters);
                await ViewModel.SetStartDateCommand.ExecuteAsync();

                ViewModel.StartTime.LocalDateTime.Date.Should().Be(selectedDate.LocalDateTime.Date);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotSetTheIsDirtyFlagWhenNothingChanges()
            {
                ViewModel.Prepare(prepareParameters);
                NavigationService
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(Arg.Any<DateTimePickerParameters>())
                    .Returns(prepareParameters.StartTime);

                await ViewModel.SetStartDateCommand.ExecuteAsync();

                ViewModel.IsDirty.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheIsDirtyFlagWhenTheStartTimeChanges()
            {
                var parameterToReturn = now.AddDays(-2);
                NavigationService
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(Arg.Any<DateTimePickerParameters>())
                    .Returns(parameterToReturn);

                ViewModel.Prepare(prepareParameters);
                await ViewModel.SetStartDateCommand.ExecuteAsync();

                ViewModel.IsDirty.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task TracksStartDateTap()
            {
                TimeService.CurrentDateTime.Returns(now);
                NavigationService
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(Arg.Any<DateTimePickerParameters>())
                    .Returns(now);
                ViewModel.Prepare(prepareParameters);

                await ViewModel.SetStartDateCommand.ExecuteAsync();

                AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.StartDate);
            }
        }

        public sealed class TheDoneCommand : StartTimeEntryViewModelTest
        {
            public sealed class StartsANewTimeEntry : StartTimeEntryViewModelTest
            {
                private const long taskId = 9;
                private const long userId = 10;
                private const long projectId = 11;
                private const long defaultWorkspaceId = 12;
                private const long projectWorkspaceId = 13;
                private const string description = "Testing Toggl apps";

                private readonly IThreadSafeUser user;
                private readonly IThreadSafeProject project;

                private readonly DateTimeOffset startDate = DateTimeOffset.UtcNow;

                public StartsANewTimeEntry()
                {
                    var defaultWorkspace = new MockWorkspace { Id = defaultWorkspaceId };
                    InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(defaultWorkspace));

                    user = new MockUser
                    {
                        Id = userId,
                        DefaultWorkspaceId = defaultWorkspaceId
                    };

                    project = new MockProject
                    {
                        Id = projectId,
                        WorkspaceId = projectWorkspaceId
                    };

                    DataSource.User.Current
                        .Returns(Observable.Return(user));

                    InteractorFactory
                         .GetProjectById(projectId)
                         .Execute()
                         .Returns(Observable.Return(project));

                    var parameter = new StartTimeEntryParameters(startDate, "", null, null);
                    ViewModel.Prepare(parameter);
                    ViewModel.Initialize().GetAwaiter().GetResult();
                }

                [Fact, LogIfTooSlow]
                public async Task CallsTheCreateTimeEntryInteractor()
                {
                    await ViewModel.DoneCommand.ExecuteAsync();

                    InteractorFactory.Received().CreateTimeEntry(ViewModel);
                }

                [Fact, LogIfTooSlow]
                public async Task ExecutesTheCreateTimeEntryInteractor()
                {
                    var mockedInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeTimeEntry>>>();
                    InteractorFactory.CreateTimeEntry(Arg.Any<ITimeEntryPrototype>()).Returns(mockedInteractor);

                    await ViewModel.DoneCommand.ExecuteAsync();

                    await mockedInteractor.Received().Execute();
                }

                [Theory, LogIfTooSlow]
                [InlineData(" ")]
                [InlineData("\t")]
                [InlineData("\n")]
                [InlineData("               ")]
                [InlineData("      \t  \n     ")]
                public async Task ReducesDescriptionConsistingOfOnlyEmptyCharactersToAnEmptyString(string description)
                {
                    await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(description, 0));

                    await ViewModel.DoneCommand.ExecuteAsync();

                    InteractorFactory.Received().CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(timeEntry =>
                        timeEntry.Description.Length == 0
                    ));
                }

                [Theory, LogIfTooSlow]
                [InlineData("   abcde", "abcde")]
                [InlineData("abcde     ", "abcde")]
                [InlineData("  abcde ", "abcde")]
                [InlineData("abcde  fgh", "abcde  fgh")]
                [InlineData("      abcd\nefgh     ", "abcd\nefgh")]
                public async Task TrimsDescriptionFromTheStartAndTheEndBeforeSaving(string description, string trimmed)
                {
                    await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(description, description.Length));

                    await ViewModel.DoneCommand.ExecuteAsync();

                    InteractorFactory.Received().CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(timeEntry =>
                        timeEntry.Description == trimmed
                    ));
                }

                [Property]
                public void DoesNotCreateARunningTimeEntryWhenDurationIsNotNull(TimeSpan duration)
                {
                    if (duration < TimeSpan.Zero) return;

                    var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", duration, null);

                    ViewModel.Prepare(parameter);
                    ViewModel.DoneCommand.ExecuteAsync().Wait();

                    InteractorFactory.Received().CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(timeEntry =>
                        timeEntry.Duration.HasValue
                    ));
                }

                [Fact, LogIfTooSlow]
                public async Task CreatesARunningTimeEntryWhenDurationIsNull()
                {
                    var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", null, null);

                    ViewModel.Prepare(parameter);
                    await ViewModel.DoneCommand.ExecuteAsync();

                    InteractorFactory.Received().CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(timeEntry =>
                        timeEntry.Duration.HasValue == false
                    ));
                }

                private TagSuggestion tagSuggestionFromInt(int i)
                {
                    var tag = Substitute.For<IThreadSafeTag>();
                    tag.Id.Returns(i);
                    tag.Name.Returns(i.ToString());

                    return new TagSuggestion(tag);
                }
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                var user = Substitute.For<IThreadSafeUser>();
                user.Id.Returns(1);
                user.DefaultWorkspaceId.Returns(10);
                DataSource.User.Current.Returns(Observable.Return(user));
                var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", null, null);
                ViewModel.Prepare(parameter);
                await ViewModel.Initialize();

                await ViewModel.DoneCommand.ExecuteAsync();

                await NavigationService.Received().Close(ViewModel);
            }
        }

        public sealed class TheSelectSuggestionCommand
        {
            public abstract class SelectSuggestionTest<TSuggestion> : StartTimeEntryViewModelTest
                where TSuggestion : AutocompleteSuggestion
            {
                protected IThreadSafeTag Tag { get; }
                protected IThreadSafeTask Task { get; }
                protected IThreadSafeProject Project { get; }
                protected IThreadSafeTimeEntry TimeEntry { get; }
                protected IThreadSafeWorkspace Workspace { get; }

                protected abstract TSuggestion Suggestion { get; }

                protected SelectSuggestionTest()
                {
                    Workspace = Substitute.For<IThreadSafeWorkspace>();
                    Workspace.Id.Returns(WorkspaceId);

                    Project = Substitute.For<IThreadSafeProject>();
                    Project.Id.Returns(ProjectId);
                    Project.Name.Returns(ProjectName);
                    Project.Color.Returns(ProjectColor);
                    Project.Workspace.Returns(Workspace);
                    Project.WorkspaceId.Returns(WorkspaceId);
                    Project.Active.Returns(true);

                    Task = Substitute.For<IThreadSafeTask>();
                    Task.Id.Returns(TaskId);
                    Task.Project.Returns(Project);
                    Task.ProjectId.Returns(ProjectId);
                    Task.WorkspaceId.Returns(WorkspaceId);
                    Task.Name.Returns(TaskId.ToString());

                    TimeEntry = Substitute.For<IThreadSafeTimeEntry>();
                    TimeEntry.Description.Returns(Description);
                    TimeEntry.Project.Returns(Project);

                    Tag = Substitute.For<IThreadSafeTag>();
                    Tag.Id.Returns(TagId);
                    Tag.Name.Returns(TagName);
                }

                [Fact, LogIfTooSlow]
                public async Task SetsTheIsDirtyFlag()
                {
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    ViewModel.IsDirty.Should().BeTrue();
                }

                [Fact, LogIfTooSlow]
                public async Task TracksProjectSelectionWhenProjectSymbolSelected()
                {
                    var projectSuggestion = QuerySymbolSuggestion.Suggestions
                        .First((s) => s.Symbol == QuerySymbols.ProjectsString);

                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(projectSuggestion);

                    AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.PickEmptyStateProjectSuggestion);
                    AnalyticsService.StartEntrySelectProject.Received().Track(ProjectTagSuggestionSource.TableCellButton);
                }

                [Fact, LogIfTooSlow]
                public async Task TracksTagSelectionWhenTagSymbolSelected()
                {
                    var tagSuggestion = QuerySymbolSuggestion.Suggestions
                        .First((s) => s.Symbol == QuerySymbols.TagsString);

                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(tagSuggestion);

                    AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.PickEmptyStateTagSuggestion);
                    AnalyticsService.StartEntrySelectTag.Received().Track(ProjectTagSuggestionSource.TableCellButton);
                }
            }

            public abstract class ProjectSettingSuggestion<TSuggestion> : SelectSuggestionTest<TSuggestion>
                where TSuggestion : AutocompleteSuggestion
            {
                [Fact, LogIfTooSlow]
                public async Task SetsTheProjectIdToTheSuggestedProjectId()
                {
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    var projectSpan = Observer.GetLatestInfo(TestScheduler).GetProjectSpan();
                    projectSpan.ProjectId.Should().Be(ProjectId);
                }

                [Fact, LogIfTooSlow]
                public async Task SetsTheProjectNameToTheSuggestedProjectName()
                {
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    var projectSpan = Observer.GetLatestInfo(TestScheduler).GetProjectSpan();
                    projectSpan.ProjectName.Should().Be(ProjectName);
                }

                [Fact, LogIfTooSlow]
                public async Task SetsTheProjectColorToTheSuggestedProjectColor()
                {
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    var projectSpan = Observer.GetLatestInfo(TestScheduler).GetProjectSpan();
                    projectSpan.ProjectColor.Should().Be(ProjectColor);
                }

                [Theory, LogIfTooSlow]
                [InlineData(true)]
                [InlineData(false)]
                [InlineData(null)]
                public async Task SetsTheAppropriateBillableValue(bool? billableValue)
                {
                    InteractorFactory.GetWorkspaceById(WorkspaceId).Execute().Returns(Observable.Return(Workspace));
                    InteractorFactory.IsBillableAvailableForProject(ProjectId).Execute()
                        .Returns(Observable.Return(true));
                    InteractorFactory.ProjectDefaultsToBillable(ProjectId).Execute()
                        .Returns(Observable.Return(billableValue ?? false));

                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    ViewModel.IsBillable.Should().Be(billableValue ?? false);
                    ViewModel.IsBillableAvailable.Should().BeTrue();
                }

                [Theory, LogIfTooSlow]
                [InlineData(true)]
                [InlineData(false)]
                [InlineData(null)]
                public async Task DisablesBillableIfTheWorkspaceOfTheSelectedProjectDoesNotAllowIt(bool? billableValue)
                {
                    Project.Billable.Returns(billableValue);
                    InteractorFactory.GetProjectById(ProjectId).Execute().Returns(Observable.Return(Project));
                    InteractorFactory.GetWorkspaceById(WorkspaceId).Execute().Returns(Observable.Return(Workspace));
                    InteractorFactory
                        .IsBillableAvailableForWorkspace(10)
                        .Execute()
                        .Returns(Observable.Return(false));

                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    ViewModel.IsBillable.Should().BeFalse();
                    ViewModel.IsBillableAvailable.Should().BeFalse();
                }
            }

            public abstract class ProjectTaskSuggestion<TSuggestion> : ProjectSettingSuggestion<TSuggestion>
                where TSuggestion : AutocompleteSuggestion
            {
                protected ProjectTaskSuggestion()
                {
                    ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("Something @togg", 15))
                        .GetAwaiter().GetResult();
                }

                [Fact, LogIfTooSlow]
                public async Task RemovesTheProjectQueryFromTheTextFieldInfo()
                {
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    var querySpan = Observer.GetLatestInfo(TestScheduler).FirstTextSpan();
                    querySpan.Text.Should().Be("Something ");
                }

                [Fact, LogIfTooSlow]
                public async Task ShowsConfirmDialogIfWorkspaceIsAboutToBeChanged()
                {
                    var user = Substitute.For<IThreadSafeUser>();
                    user.DefaultWorkspaceId.Returns(100);
                    DataSource.User.Current.Returns(Observable.Return(user));
                    ViewModel.Prepare();
                    await ViewModel.Initialize();

                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    await DialogService.Received().Confirm(
                        Arg.Is(Resources.DifferentWorkspaceAlertTitle),
                        Arg.Is(Resources.DifferentWorkspaceAlertMessage),
                        Arg.Is(Resources.Ok),
                        Arg.Is(Resources.Cancel)
                    );
                }

                [Fact, LogIfTooSlow]
                public async Task DoesNotShowConfirmDialogIfWorkspaceIsNotGoingToChange()
                {
                    var workspace = new MockWorkspace { Id = WorkspaceId };
                    InteractorFactory.GetDefaultWorkspace().Execute().Returns(Observable.Return(workspace));
                    var user = Substitute.For<IThreadSafeUser>();
                    user.DefaultWorkspaceId.Returns(WorkspaceId);
                    DataSource.User.Current.Returns(Observable.Return(user));
                    ViewModel.Prepare();
                    await ViewModel.Initialize();

                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    await DialogService.DidNotReceive().Confirm(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<string>()
                    );
                }

                [Fact, LogIfTooSlow]
                public async Task ClearsTagsIfWorkspaceIsChanged()
                {
                    var user = Substitute.For<IThreadSafeUser>();
                    user.DefaultWorkspaceId.Returns(100);
                    DataSource.User.Current.Returns(Observable.Return(user));
                    ViewModel.Prepare();
                    await ViewModel.Initialize();
                    Enumerable.Range(100, 10)
                        .Select(i =>
                        {
                            var tag = Substitute.For<IThreadSafeTag>();
                            tag.Id.Returns(i);
                            return new TagSuggestion(tag);
                        })
                        .ForEach(ViewModel.SelectSuggestionCommand.Execute);

                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    var tags = Observer.GetLatestInfo(TestScheduler).Spans.OfType<TagSpan>();
                    tags.Should().BeEmpty();
                }
            }

            public sealed class WhenSelectingATimeEntrySuggestion : ProjectSettingSuggestion<TimeEntrySuggestion>
            {
                protected override TimeEntrySuggestion Suggestion { get; }

                public WhenSelectingATimeEntrySuggestion()
                {
                    Suggestion = new TimeEntrySuggestion(TimeEntry);
                }

                [Fact, LogIfTooSlow]
                public async Task SetsTheTextFieldInfoTextToTheValueOfTheSuggestedDescription()
                {
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    var querySpan = Observer.GetLatestInfo(TestScheduler).FirstTextSpan();
                    querySpan.Text.Should().Be(Description);
                }

                [Fact, LogIfTooSlow]
                public async Task TracksWhenTimeEntrySuggestionSelected()
                {
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.PickTimeEntrySuggestion);
                }

                [Fact, LogIfTooSlow]
                public async Task ChangesTheWorkspaceIfNeeded()
                {
                    const long expectedWorkspaceId = WorkspaceId + 1;
                    TimeEntry.WorkspaceId.Returns(expectedWorkspaceId);
                    var newSuggestion = new TimeEntrySuggestion(TimeEntry);

                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(newSuggestion);

                    Observer.GetLatestInfo(TestScheduler).WorkspaceId.Should().Be(expectedWorkspaceId);
                }
            }

            public sealed class WhenSelectingATaskSuggestion : ProjectTaskSuggestion<TaskSuggestion>
            {
                protected override TaskSuggestion Suggestion { get; }

                public WhenSelectingATaskSuggestion()
                {
                    Suggestion = new TaskSuggestion(Task);
                }

                [Fact, LogIfTooSlow]
                public async Task SetsTheTaskIdToTheSameIdAsTheSelectedSuggestion()
                {
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    var projectSpan = Observer.GetLatestInfo(TestScheduler).GetProjectSpan();
                    projectSpan.TaskId.Should().Be(TaskId);
                }

                [Fact, LogIfTooSlow]
                public async Task TracksWhenTaskSuggestionSelected()
                {
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.PickTaskSuggestion);
                }
            }

            public sealed class WhenSelectingAProjectSuggestion : ProjectTaskSuggestion<ProjectSuggestion>
            {
                protected override ProjectSuggestion Suggestion { get; }

                public WhenSelectingAProjectSuggestion()
                {
                    Suggestion = new ProjectSuggestion(Project);
                }

                [Fact, LogIfTooSlow]
                public async Task SetsTheTaskIdToNull()
                {
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    var projectSpan = Observer.GetLatestInfo(TestScheduler).GetProjectSpan();
                    projectSpan.TaskId.Should().BeNull();
                }

                [Theory, LogIfTooSlow]
                [InlineData(true)]
                [InlineData(false)]
                public async Task SetsTheAppropriateBillableValueBasedOnTheWorkspaceWhenSelectingNoProject(bool isBillableAvailable)
                {
                    InteractorFactory.GetWorkspaceById(WorkspaceId).Execute().Returns(Observable.Return(Workspace));
                    InteractorFactory.IsBillableAvailableForProject(ProjectId).Execute()
                        .Returns(Observable.Throw<bool>(new DatabaseOperationException<IDatabaseProject>(new Exception())));
                    InteractorFactory.IsBillableAvailableForWorkspace(WorkspaceId).Execute()
                        .Returns(Observable.Return(isBillableAvailable));
                    var noProjectSuggestion = ProjectSuggestion.NoProject(WorkspaceId, Workspace.Name);

                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(noProjectSuggestion);

                    ViewModel.IsBillable.Should().BeFalse();
                    ViewModel.IsBillableAvailable.Should().Be(isBillableAvailable);
                }

                [Fact, LogIfTooSlow]
                public async Task TracksWhenProjectSuggestionSelected()
                {
                    DialogService
                        .Confirm(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                        .Returns(Observable.Return(false));

                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.PickProjectSuggestion);
                }
            }

            public sealed class WhenSelectingATagSuggestion : SelectSuggestionTest<TagSuggestion>
            {
                protected override TagSuggestion Suggestion { get; }

                public WhenSelectingATagSuggestion()
                {
                    Suggestion = new TagSuggestion(Tag);

                    ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("Something #togg", 15))
                        .GetAwaiter().GetResult();
                }

                [Fact, LogIfTooSlow]
                public async Task RemovesTheTagQueryFromTheTextFieldInfo()
                {
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    var querySpan = Observer.GetLatestInfo(TestScheduler).Spans.OfType<TextSpan>().First();
                    querySpan.Text.Should().Be("Something ");
                }

                [Fact, LogIfTooSlow]
                public async Task AddsTheSuggestedTagToTheList()
                {
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    var tags = Observer.GetLatestInfo(TestScheduler).Spans.OfType<TagSpan>();
                    tags.Should().Contain(t => t.TagId == Suggestion.TagId);
                }

                [Fact, LogIfTooSlow]
                public async Task TracksWhenTagSuggestionSelected()
                {
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.PickTagSuggestion);
                }
            }

            public sealed class WhenSelectingAQuerySymbolSuggestion : SelectSuggestionTest<QuerySymbolSuggestion>
            {
                protected override QuerySymbolSuggestion Suggestion { get; } = QuerySymbolSuggestion.Suggestions.First();

                [Fact, LogIfTooSlow]
                public async Task SetsTheTextToTheQuerySymbolSelected()
                {
                    await ViewModel.SelectSuggestionCommand.ExecuteAsync(Suggestion);

                    var querySpan = Observer.GetLatestInfo(TestScheduler).FirstTextSpan();
                    querySpan.Text.Should().Be(Suggestion.Symbol);
                }
            }
        }

        public sealed class TheSelectTimeCommand : StartTimeEntryViewModelTest
        {
            private const SelectTimeParameters.Origin origin = Duration;
            private readonly TaskCompletionSource<SelectTimeResultsParameters> tcs = new TaskCompletionSource<SelectTimeResultsParameters>();

            public TheSelectTimeCommand()
            {
                NavigationService
                    .Navigate<SelectTimeViewModel, SelectTimeParameters, SelectTimeResultsParameters>(Arg.Any<SelectTimeParameters>())
                    .Returns(tcs.Task);
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheSelectViewModelWithACalculatedStopDateIfTheDurationIsNotNull()
            {
                ViewModel.Prepare(StartTimeEntryParameters.ForManualMode(DateTimeOffset.Now));

                await callCommandCorrectly();

                await NavigationService
                    .Received()
                    .Navigate<SelectTimeViewModel, SelectTimeParameters, SelectTimeResultsParameters>(Arg.Is<SelectTimeParameters>(
                        parameters => parameters.Stop != null
                    ));
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheSelectViewModelWithNoStopDateIfTheDurationIsNull()
            {
                ViewModel.Prepare(StartTimeEntryParameters.ForTimerMode(DateTimeOffset.Now));

                await callCommandCorrectly();

                await NavigationService
                    .Received()
                    .Navigate<SelectTimeViewModel, SelectTimeParameters, SelectTimeResultsParameters>(Arg.Is<SelectTimeParameters>(
                        parameters => parameters.Stop == null
                    ));
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheDurationIfTheStopResultHasAValue()
            {
                const int totalDurationInHours = 2;
                ViewModel.Prepare(StartTimeEntryParameters.ForTimerMode(DateTimeOffset.Now));

                await callCommandCorrectly(totalDurationInHours);

                ViewModel.Duration.Value.TotalHours.Should().Be(totalDurationInHours);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotSetTheDurationIfTheStopResultHasNoValue()
            {
                ViewModel.Prepare(StartTimeEntryParameters.ForTimerMode(DateTimeOffset.Now));

                await callCommandCorrectly();

                ViewModel.Duration.Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheStartDateToTheValueReturned()
            {
                const int totalDurationInHours = 2;
                ViewModel.Prepare(StartTimeEntryParameters.ForTimerMode(DateTimeOffset.Now));

                await callCommandCorrectly(totalDurationInHours);
                var expected = (await tcs.Task).Start;

                ViewModel.StartTime.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public async Task TracksDurationTap()
            {
                var now = DateTimeOffset.UtcNow;
                var parameters = StartTimeEntryParameters.ForTimerMode(now);
                var returnParameters = new SelectTimeResultsParameters(now, null);
                NavigationService
                    .Navigate<SelectTimeViewModel, SelectTimeParameters, SelectTimeResultsParameters>(Arg.Any<SelectTimeParameters>())
                    .Returns(Task.FromResult(returnParameters));
                ViewModel.Prepare(parameters);

                await ViewModel.SelectTimeCommand.ExecuteAsync(SelectTimeParameters.Origin.Duration);

                AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.Duration);
            }

            private Task callCommandCorrectly(int? hoursToAddToStopTime = null)
            {
                var commandTask = ViewModel.SelectTimeCommand.ExecuteAsync(origin);
                var now = DateTimeOffset.Now;
                var stopTime = hoursToAddToStopTime.HasValue ? now.AddHours(hoursToAddToStopTime.Value) : (DateTimeOffset?)null;
                tcs.SetResult(new SelectTimeResultsParameters(now, stopTime));
                return commandTask;
            }
        }

        public sealed class TheDurationTappedCommand : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void TracksDurationTap()
            {
                ViewModel.DurationTapped.Execute();

                AnalyticsService.StartViewTapped.Received().Track(StartViewTapSource.Duration);
            }
        }

        public sealed class TheSuggestionsProperty : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task IsClearedWhenThereAreNoWordsToQuery()
            {
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("", 0));

                ViewModel.Suggestions.Should().HaveCount(0);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotSuggestAnythingWhenAProjectIsAlreadySelected()
            {
                var description = "abc";
                var projectA = Substitute.For<IThreadSafeProject>();
                projectA.Id.Returns(ProjectId);
                var projectB = Substitute.For<IThreadSafeProject>();
                projectB.Id.Returns(ProjectId + 1);
                var timeEntryA = Substitute.For<IThreadSafeTimeEntry>();
                timeEntryA.Description.Returns(description);
                timeEntryA.Project.Returns(projectA);
                var timeEntryB = Substitute.For<IThreadSafeTimeEntry>();
                timeEntryB.Description.Returns(description);
                timeEntryB.Project.Returns(projectB);
                var suggestions = Observable.Return(new AutocompleteSuggestion[]
                    {
                        new TimeEntrySuggestion(timeEntryA),
                        new TimeEntrySuggestion(timeEntryB)
                    });
                AutocompleteProvider.Query(Arg.Any<QueryInfo>()).Returns(suggestions);
                ViewModel.Prepare();
                TestScheduler.Start();
                ViewModel.Prepare(DefaultParameter);

                await ViewModel.OnTextFieldInfoFromView(
                    new QueryTextSpan(description, description.Length),
                    new ProjectSpan(ProjectId, ProjectName, ProjectColor)
                );
                TestScheduler.Start();

                ViewModel.Suggestions.Should().HaveCount(1);
                ViewModel.Suggestions[0].Should().HaveCount(1);
                var suggestion = ViewModel.Suggestions[0][0];
                suggestion.Should().BeOfType<TimeEntrySuggestion>();
                ((TimeEntrySuggestion)suggestion).ProjectId.Should().Be(ProjectId);
            }
        }

        public sealed class TheTextFieldInfoProperty : StartTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("abc @def")]
            [InlineData("abc #def")]
            public async Task DoesNotChangeSuggestionsWhenOnlyTheCursorMovesForward(string text)
            {
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(text, text.Length));
                AutocompleteProvider.ClearReceivedCalls();

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(text, 0));

                await AutocompleteProvider.DidNotReceive().Query(Arg.Any<QueryInfo>());
            }

            [Theory, LogIfTooSlow]
            [InlineData("abc @def")]
            [InlineData("abc #def")]
            public async Task ChangesSuggestionsWhenTheCursorMovesBackBehindTheOldCursorPosition(string text)
            {
                var extendedText = text + "x";
                ViewModel.Prepare();
                TestScheduler.Start();
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(extendedText, text.Length));
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(extendedText, 0));
                AutocompleteProvider.ClearReceivedCalls();

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(extendedText, extendedText.Length));
                TestScheduler.Start();

                await AutocompleteProvider.Received().Query(Arg.Any<QueryInfo>());
            }

            [Theory, LogIfTooSlow]
            [InlineData("abc @def")]
            [InlineData("abc #def")]
            public async Task ChangesSuggestionsWhenTheCursorMovesBeforeTheQuerySymbolAndUserStartsTyping(string text)
            {
                ViewModel.Prepare();
                TestScheduler.Start();
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(text, text.Length));
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(text, 0));
                AutocompleteProvider.ClearReceivedCalls();

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("x" + text, 1));
                TestScheduler.Start();

                await AutocompleteProvider.Received().Query(Arg.Is<QueryInfo>(query => query.Text.StartsWith("x")));
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotSetTheIsDirtyFlagIfTheTextFieldIsEmpty()
            {
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan());

                ViewModel.IsDirty.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheIsDirtyFlag()
            {
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("a", 1));

                ViewModel.IsDirty.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task ClearsTheIsDirtyFlagIfTheTextFieldIsErased()
            {
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan("a", 1));
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan());

                ViewModel.IsDirty.Should().BeFalse();
            }
        }

        public sealed class TheDescriptionRemainingBytesProperty : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task IsMaxIfTheTextIsEmpty()
            {
                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan());

                ViewModel.DescriptionRemainingBytes.Should()
                    .Be(MaxTimeEntryDescriptionLengthInBytes);
            }

            [Theory, LogIfTooSlow]
            [InlineData("Hello fox")]
            [InlineData("Some emojis: 🔥😳👻")]
            [InlineData("Some weird characters: āčēļķīņš")]
            [InlineData("Some random arabic characters: ظۓڰڿڀ")]
            public async Task IsDecreasedForEachByteInTheText(string text)
            {
                var expectedRemainingByteCount
                    = MaxTimeEntryDescriptionLengthInBytes - text.LengthInBytes();

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(text, 0));

                ViewModel.DescriptionRemainingBytes.Should()
                    .Be(expectedRemainingByteCount);
            }

            [Fact, LogIfTooSlow]
            public async Task IsNegativeWhenTextLengthExceedsMax()
            {
                var bytesOverLimit = 5;
                var longString = new string('0', MaxTimeEntryDescriptionLengthInBytes + bytesOverLimit);

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(longString, 0));

                ViewModel.DescriptionRemainingBytes.Should().Be(-bytesOverLimit);
            }
        }

        public sealed class TheDescriptionLengthExceededproperty : StartTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(0)]
            [InlineData(20)]
            [InlineData(2999)]
            [InlineData(3000)]
            public async Task IsFalseIfTextIsShorterOrEqualToMax(int byteCount)
            {
                var text = new string('0', byteCount);

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(text, 0));

                ViewModel.DescriptionLengthExceeded.Should().BeFalse();
            }

            [Theory, LogIfTooSlow]
            [InlineData(MaxTimeEntryDescriptionLengthInBytes + 1)]
            [InlineData(MaxTimeEntryDescriptionLengthInBytes + 20)]
            public async Task IsTrueWhenTextIsLongerThanMax(int byteCount)
            {
                var text = new string('0', byteCount);

                await ViewModel.OnTextFieldInfoFromView(new QueryTextSpan(text, 0));

                ViewModel.DescriptionLengthExceeded.Should().BeTrue();
            }
        }

        public sealed class TheShouldShowNoTagsInfoMessage : StartTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("")]
            [InlineData("asd ")]
            [InlineData("\tasd asd ")]
            [InlineData("x")]
            public async Task ReturnsTrueWhenSuggestingTagsAndUserHasNoTags(string query)
            {
                ViewModel.Prepare();
                TestScheduler.Start();
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.Initialize();
                await ViewModel.OnTextFieldInfoFromView(
                    new QueryTextSpan($"{QuerySymbols.Tags}{query}", 1)
                );

                ViewModel.ShouldShowNoTagsInfoMessage.Should().BeTrue();
            }

            [Theory, LogIfTooSlow]
            [InlineData(1, " ")]
            [InlineData(2, "#tag")]
            [InlineData(8, "@Project")]
            [InlineData(3, "Time entry")]
            [InlineData(1, "#")]
            public async Task ReturnsFalseInAnyOtherCase(int tagCount, string query)
            {
                var tags = Enumerable
                    .Range(0, tagCount)
                    .Select(_ => Substitute.For<IThreadSafeTag>());
                DataSource.Tags.GetAll().Returns(Observable.Return(tags));
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.Initialize();
                await ViewModel.OnTextFieldInfoFromView(
                    new QueryTextSpan(query, 1)
                );

                ViewModel.ShouldShowNoTagsInfoMessage.Should().BeFalse();
            }

            [Theory, LogIfTooSlow]
            [InlineData("")]
            [InlineData("asd ")]
            [InlineData("\tasd asd ")]
            [InlineData("x")]
            public async Task ReturnsFalseAfterCreatingATag(string query)
            {
                var tag = Substitute.For<IThreadSafeTag>();
                InteractorFactory.CreateTag(Arg.Any<string>(), Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(tag));
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.Initialize();
                await ViewModel.OnTextFieldInfoFromView(
                    new QueryTextSpan($"{QuerySymbols.Tags}{query}", 1)
                );

                ViewModel.CreateCommand.Execute();

                ViewModel.ShouldShowNoTagsInfoMessage.Should().BeFalse();
            }
        }
    }

    public static class TestExtensions
    {
        public static Task OnTextFieldInfoFromView(this StartTimeEntryViewModel viewModel, params ISpan[] spans)
            => viewModel.OnTextFieldInfoFromView(spans.ToImmutableList());

        public static TextFieldInfo GetLatestInfo(this ITestableObserver<TextFieldInfo> observer, TestScheduler testScheduler)
        {
            testScheduler.Start();
            return observer.Messages.Last().Value.Value;
        }

        public static QueryTextSpan GetQuerySpan(this TextFieldInfo textFieldInfo)
            => textFieldInfo.Spans.OfType<QueryTextSpan>().Single();

        public static ProjectSpan GetProjectSpan(this TextFieldInfo textFieldInfo)
            => textFieldInfo.Spans.OfType<ProjectSpan>().Single();

        public static TextSpan FirstTextSpan(this TextFieldInfo textFieldInfo)
            => textFieldInfo.Spans.OfType<TextSpan>().First();
    }
}

