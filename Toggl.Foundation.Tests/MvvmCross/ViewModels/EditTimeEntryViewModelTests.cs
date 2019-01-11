using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Models;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Tests.Mocks;
using Xunit;
using static Toggl.Foundation.Helper.Constants;
using static Toggl.Multivac.Extensions.StringExtensions;
using Task = System.Threading.Tasks.Task;
using Toggl.Foundation.Tests.Mocks;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class EditTimeEntryViewModelTests
    {
        public abstract class EditTimeEntryViewModelTest : BaseViewModelTests<EditTimeEntryViewModel>
        {
            protected const long Id = 10;

            protected readonly TimeSpan Duration = TimeSpan.FromHours(1);

            protected IThreadSafeTimeEntry TheTimeEntry;

            protected void ConfigureEditedTimeEntry(DateTimeOffset now, bool isRunning = false)
            {
                var workspace = new MockWorkspace { Id = 11 };
                var user = new MockUser { Id = 12 };
                var project = new MockProject { Id = 13 };
                var task = new MockTask { Id = 14 };

                TheTimeEntry = new MockTimeEntry
                {
                    Id = Id,
                    Task = task,
                    TaskId = task.Id,
                    UserId = user.Id,
                    Project = project,
                    TagIds = new long[0],
                    Workspace = workspace,
                    ProjectId = project.Id,
                    At = now.AddHours(-2),
                    Start = now.AddHours(-2),
                    Description = "Something",
                    WorkspaceId = workspace.Id,
                    Duration = isRunning ? (long?)null : (long)Duration.TotalSeconds
                };

                var observable = Observable.Return(TheTimeEntry);

                InteractorFactory.GetTimeEntryById(Arg.Is(Id)).Execute().Returns(observable);

                TimeService.CurrentDateTime.Returns(now);
            }

            protected override EditTimeEntryViewModel CreateViewModel()
            => new EditTimeEntryViewModel(TimeService, DataSource, InteractorFactory, NavigationService, OnboardingStorage, DialogService, AnalyticsService, StopwatchProvider);
        }

        public sealed class TheConstructor : EditTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useNavigationService,
                bool useTimeService,
                bool useInteractorFactory,
                bool useOnboardingStorage,
                bool useDialogService,
                bool useAnalyticsService,
                bool useStopwatchProvider)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var dialogService = useDialogService ? DialogService : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var stopwatchProvider = useStopwatchProvider ? StopwatchProvider : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new EditTimeEntryViewModel(timeService, dataSource, interactorFactory, navigationService, onboardingStorage, dialogService, analyticsService, stopwatchProvider);

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheCloseCommand : EditTimeEntryViewModelTest
        {
            public TheCloseCommand()
            {
                ConfigureEditedTimeEntry(DateTimeOffset.Now, true);

                ViewModel.Prepare(Id);
                ViewModel.Initialize().Wait();
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModelIfNothingChanged()
            {
                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received().Close(Arg.Is(ViewModel));
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheConfirmationDialogIfDescriptionChanges()
            {
                ViewModel.Description = "Something Else";

                await ViewModel.CloseCommand.ExecuteAsync();

                await DialogService.Received().ConfirmDestructiveAction(ActionType.DiscardEditingChanges);
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheConfirmationDialogIfProjectChanges()
            {
                var selectProjectParameter = SelectProjectParameter.WithIds(TheTimeEntry.ProjectId + 1, TheTimeEntry.TaskId, TheTimeEntry.WorkspaceId);
                NavigationService
                    .Navigate<SelectProjectViewModel, SelectProjectParameter, SelectProjectParameter>(
                        Arg.Any<SelectProjectParameter>())
                    .Returns(selectProjectParameter);

                await ViewModel.SelectProjectCommand.ExecuteAsync();
                await ViewModel.CloseCommand.ExecuteAsync();

                await DialogService.Received().ConfirmDestructiveAction(ActionType.DiscardEditingChanges);
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheConfirmationDialogIfTaskChanges()
            {
                var selectProjectParameter = SelectProjectParameter.WithIds(TheTimeEntry.ProjectId, TheTimeEntry.TaskId + 1, TheTimeEntry.WorkspaceId);
                NavigationService
                    .Navigate<SelectProjectViewModel, SelectProjectParameter, SelectProjectParameter>(
                        Arg.Any<SelectProjectParameter>())
                    .Returns(selectProjectParameter);

                await ViewModel.SelectProjectCommand.ExecuteAsync();
                await ViewModel.CloseCommand.ExecuteAsync();

                await DialogService.Received().ConfirmDestructiveAction(ActionType.DiscardEditingChanges);
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheConfirmationDialogIfWorkspaceChanges()
            {
                var selectProjectParameter = SelectProjectParameter.WithIds(TheTimeEntry.ProjectId, TheTimeEntry.TaskId, TheTimeEntry.WorkspaceId + 1);
                NavigationService
                    .Navigate<SelectProjectViewModel, SelectProjectParameter, SelectProjectParameter>(
                        Arg.Any<SelectProjectParameter>())
                    .Returns(selectProjectParameter);

                await ViewModel.SelectProjectCommand.ExecuteAsync();
                await ViewModel.CloseCommand.ExecuteAsync();

                await DialogService.Received().ConfirmDestructiveAction(ActionType.DiscardEditingChanges);
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheConfirmationDialogIfTheStartTimeChanges()
            {
                var newStartTime = TheTimeEntry.Start.AddHours(1);
                var newDurationParameter = DurationParameter.WithStartAndDuration(newStartTime, null);
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(
                        Arg.Any<EditDurationParameters>())
                    .Returns(newDurationParameter);

                await ViewModel.SelectStartTimeCommand.ExecuteAsync();
                await ViewModel.CloseCommand.ExecuteAsync();

                await DialogService.Received().ConfirmDestructiveAction(ActionType.DiscardEditingChanges);
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheConfirmationDialogIfTheDurationChanges()
            {
                ViewModel.StopCommand.Execute();

                await ViewModel.CloseCommand.ExecuteAsync();

                await DialogService.Received().ConfirmDestructiveAction(ActionType.DiscardEditingChanges);
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheConfirmationDialogIfTheBillableFlagChanges()
            {
                ViewModel.Billable = !ViewModel.Billable;

                await ViewModel.CloseCommand.ExecuteAsync();

                await DialogService.Received().ConfirmDestructiveAction(ActionType.DiscardEditingChanges);
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsTheConfirmationDialogIfTagsChange()
            {
                var newTags = new long[] { 1, 2, 3 };
                NavigationService
                    .Navigate<SelectTagsViewModel, (long[], long), long[]>(
                        Arg.Any<(long[], long)>())
                    .Returns(newTags);

                await ViewModel.SelectTagsCommand.ExecuteAsync();
                await ViewModel.CloseCommand.ExecuteAsync();

                await DialogService.Received().ConfirmDestructiveAction(ActionType.DiscardEditingChanges);
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewIfUserClicksOnTheDiscardButton()
            {
                DialogService
                    .ConfirmDestructiveAction(ActionType.DiscardEditingChanges)
                    .Returns(Observable.Return(true));

                ViewModel.Billable = !ViewModel.Billable;
                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received().Close(ViewModel);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotCloseTheViewIfUserClicksOnTheContinueEditingButton()
            {
                DialogService
                    .ConfirmDestructiveAction(ActionType.DiscardEditingChanges)
                    .Returns(Observable.Return(false));

                ViewModel.Billable = !ViewModel.Billable;
                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.DidNotReceive().Close(ViewModel);
            }
        }

        public class TheDeleteCommand : EditTimeEntryViewModelTest
        {
            protected void PrepareActionSheet(bool confirm)
            {
                DialogService.ConfirmDestructiveAction(ActionType.DeleteExistingTimeEntry)
                    .Returns(Observable.Return(confirm));
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsConfirmationActionSheet()
            {
                await ViewModel.DeleteCommand.ExecuteAsync();

                await DialogService.Received().ConfirmDestructiveAction(ActionType.DeleteExistingTimeEntry);
            }

            public sealed class WhenUserConfirms : TheDeleteCommand
            {
                public WhenUserConfirms()
                {
                    PrepareActionSheet(true);
                }

                [Fact, LogIfTooSlow]
                public async Task ExecutesTheDeleteInteractor()
                {
                    await ViewModel.DeleteCommand.ExecuteAsync();

                    InteractorFactory.Received().DeleteTimeEntry(Arg.Any<long>());
                    await InteractorFactory.DeleteTimeEntry(Arg.Any<long>()).Received().Execute();
                }

                [Fact, LogIfTooSlow]
                public async Task InitiatesPushSync()
                {
                    await ViewModel.DeleteCommand.ExecuteAsync();

                    await DataSource.SyncManager.Received().PushSync();
                }

                [Fact, LogIfTooSlow]
                public async Task DoesNotInitiatePushSyncWhenDeletingFails()
                {
                    var interactor = Substitute.For<IInteractor<IObservable<Unit>>>();
                    interactor.Execute()
                        .Returns(Observable.Throw<Unit>(new Exception()));
                    InteractorFactory.DeleteTimeEntry(Arg.Any<long>())
                        .Returns(interactor);

                    await ViewModel.DeleteCommand.ExecuteAsync();

                    await DataSource.SyncManager.DidNotReceive().PushSync();
                }

                [Fact]
                public async Task TracksTheEventUsingTheAnaltyticsService()
                {
                    await ViewModel.DeleteCommand.ExecuteAsync();

                    AnalyticsService.Received().DeleteTimeEntry.Track();
                }
            }

            public sealed class WhenUserCancels : TheDeleteCommand
            {
                public WhenUserCancels()
                {
                    PrepareActionSheet(false);
                }

                [Fact, LogIfTooSlow]
                public async Task DoesNotCallDeleteOnDataSource()
                {
                    await ViewModel.DeleteCommand.ExecuteAsync();

                    await DataSource.TimeEntries.DidNotReceive().Delete(Arg.Is(ViewModel.Id));
                }

                [Fact, LogIfTooSlow]
                public async Task DoesNotInitiatePushSync()
                {
                    await ViewModel.DeleteCommand.ExecuteAsync();

                    await DataSource.SyncManager.DidNotReceive().PushSync();
                }
            }
        }

        public sealed class TheStopCommand : EditTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void CannotBeExecutedForAStoppedTimeEntry()
            {
                ConfigureEditedTimeEntry(DateTimeOffset.UtcNow, false);
                ViewModel.Prepare(Id);
                ViewModel.Initialize().Wait();

                var canExecute = ViewModel.StopCommand.CanExecute();

                canExecute.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void CanBeExecutedForARunningTimeEntry()
            {
                ConfigureEditedTimeEntry(DateTimeOffset.UtcNow, true);
                ViewModel.Prepare(Id);
                ViewModel.Initialize().Wait();

                var canExecute = ViewModel.StopCommand.CanExecute();

                canExecute.Should().BeTrue();
            }

            [Property]
            public void SetsTheCurrentTimeAsTheStopTime(DateTimeOffset now)
            {
                ConfigureEditedTimeEntry(now, true);
                ViewModel.Prepare(Id);
                ViewModel.Initialize().Wait();

                ViewModel.StopCommand.Execute();

                ViewModel.StopTime.Should().Be(now);
            }

            [Fact, LogIfTooSlow]
            public void ClearsTheIsRunningFlag()
            {
                ConfigureEditedTimeEntry(DateTimeOffset.UtcNow, true);
                ViewModel.Prepare(Id);
                ViewModel.Initialize().Wait();

                ViewModel.StopCommand.Execute();

                ViewModel.IsTimeEntryRunning.Should().BeFalse();
            }
        }

        public sealed class TheEditDurationCommand : EditTimeEntryViewModelTest
        {
            [Property]
            public void SetsTheStartTimeToTheValueReturnedByTheSelectDateTimeDialogViewModelWhenEditingARunningTimeEntry(DateTimeOffset now)
            {
                var parameterToReturn = DurationParameter.WithStartAndDuration(now.AddHours(-3), null);
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(Arg.Any<EditDurationParameters>())
                    .Returns(parameterToReturn);
                ConfigureEditedTimeEntry(now);
                ViewModel.Prepare(Id);

                ViewModel.SelectDurationCommand.ExecuteAsync().Wait();

                ViewModel.StartTime.Should().Be(parameterToReturn.Start);
            }

            [Property]
            public void SetsTheStopTimeToTheValueReturnedByTheSelectDateTimeDialogViewModelWhenEditingACompletedTimeEntry(DateTimeOffset now)
            {
                var start = now.AddHours(-4);
                var duration = TimeSpan.FromHours(1);
                var parameterToReturn = DurationParameter.WithStartAndDuration(start, duration);
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(Arg.Any<EditDurationParameters>())
                    .Returns(parameterToReturn);
                ConfigureEditedTimeEntry(now);
                ViewModel.Prepare(Id);

                ViewModel.SelectDurationCommand.ExecuteAsync().Wait();

                ViewModel.Duration.Should().Be(parameterToReturn.Duration.Value);
            }

            [Fact, LogIfTooSlow]
            public void TracksDurationTap()
            {
                ConfigureEditedTimeEntry(DateTimeOffset.Now);
                ViewModel.Prepare(Id);
                ViewModel.Initialize().Wait();

                var newStartTime = TheTimeEntry.Start.AddHours(1);
                var newDurationParameter = DurationParameter.WithStartAndDuration(newStartTime, null);
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(
                        Arg.Any<EditDurationParameters>())
                    .Returns(newDurationParameter);

                ViewModel.SelectDurationCommand.Execute();

                AnalyticsService.Received()
                                .EditViewTapped
                                .Track(Arg.Is(EditViewTapSource.Duration));
            }
        }

        public sealed class TheEditDurationAndShowNumpadCommand : EditTimeEntryViewModelTest
        {
            [Property]
            public void SetsTheStartTimeToTheValueReturnedByTheSelectDateTimeDialogViewModelWhenEditingARunningTimeEntry(DateTimeOffset now)
            {
                var parameterToReturn = DurationParameter.WithStartAndDuration(now.AddHours(-3), null);
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(Arg.Any<EditDurationParameters>())
                    .Returns(parameterToReturn);
                ConfigureEditedTimeEntry(now);
                ViewModel.Prepare(Id);

                ViewModel.SelectDurationCommand.ExecuteAsync().Wait();

                ViewModel.StartTime.Should().Be(parameterToReturn.Start);
            }

            [Property]
            public void SetsTheStopTimeToTheValueReturnedByTheSelectDateTimeDialogViewModelWhenEditingACompletedTimeEntry(DateTimeOffset now)
            {
                var start = now.AddHours(-4);
                var duration = TimeSpan.FromHours(1);
                var parameterToReturn = DurationParameter.WithStartAndDuration(start, duration);
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(Arg.Any<EditDurationParameters>())
                    .Returns(parameterToReturn);
                ConfigureEditedTimeEntry(now);
                ViewModel.Prepare(Id);

                ViewModel.SelectDurationCommand.ExecuteAsync().Wait();

                ViewModel.Duration.Should().Be(parameterToReturn.Duration.Value);
            }
        }

        public sealed class TheSaveCommand : EditTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void SetsTheOnboardingStorageFlag()
            {
                ViewModel.SaveCommand.Execute();

                OnboardingStorage.Received().EditedTimeEntry();
            }

            [Fact, LogIfTooSlow]
            public async Task InitiatesPushSync()
            {
                ViewModel.SaveCommand.Execute();

                await DataSource.SyncManager.Received().PushSync();
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotInitiatePushSyncWhenSavingFails()
            {
                InteractorFactory.UpdateTimeEntry(Arg.Any<EditTimeEntryDto>())
                    .Execute()
                    .Returns(Observable.Throw<IThreadSafeTimeEntry>(new Exception()));

                ViewModel.SaveCommand.Execute();

                await DataSource.SyncManager.DidNotReceive().PushSync();
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesWorkspaceIdIfProjectFromAnotherWorkspaceWasSelected()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(10);
                timeEntry.WorkspaceId.Returns(11);
                timeEntry.ProjectId.Returns(12);
                InteractorFactory.GetTimeEntryById(Arg.Is(timeEntry.Id))
                    .Execute()
                    .Returns(Observable.Return(timeEntry));
                var newProjectId = 20;
                var project = Substitute.For<IThreadSafeProject>();
                project.Id.Returns(newProjectId);
                project.WorkspaceId.Returns(21);
                InteractorFactory.GetProjectById(project.Id)
                    .Execute()
                    .Returns(Observable.Return(project));
                ViewModel.Prepare(timeEntry.Id);
                await ViewModel.Initialize();
                var parameter = SelectProjectParameter.WithIds(newProjectId, null, project.WorkspaceId);
                NavigationService.Navigate<SelectProjectViewModel, SelectProjectParameter, SelectProjectParameter>(
                        Arg.Any<SelectProjectParameter>())
                    .Returns(parameter);
                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.SaveCommand.Execute();

                await InteractorFactory.Received()
                    .UpdateTimeEntry(Arg.Is<EditTimeEntryDto>(dto => dto.WorkspaceId == project.WorkspaceId))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotUpdateWorkspaceIdIfProjectFromTheSameWorkspaceIsSelected()
            {
                var workspaceId = 11;
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(10);
                timeEntry.WorkspaceId.Returns(workspaceId);
                timeEntry.ProjectId.Returns(12);
                InteractorFactory.GetTimeEntryById(Arg.Is(timeEntry.Id))
                    .Execute()
                    .Returns(Observable.Return(timeEntry));
                var newProjectId = 20;
                var project = Substitute.For<IThreadSafeProject>();
                project.Id.Returns(newProjectId);
                project.WorkspaceId.Returns(workspaceId);
                InteractorFactory.GetProjectById(project.Id)
                    .Execute()
                    .Returns(Observable.Return(project));
                ViewModel.Prepare(timeEntry.Id);
                await ViewModel.Initialize();
                NavigationService.Navigate<SelectProjectViewModel, SelectProjectParameter, SelectProjectParameter>(
                        Arg.Any<SelectProjectParameter>())
                    .Returns(SelectProjectParameter.WithIds(newProjectId, null, workspaceId));
                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.SaveCommand.Execute();

                await InteractorFactory
                    .Received()
                    .UpdateTimeEntry(Arg.Is<EditTimeEntryDto>(dto => dto.WorkspaceId == workspaceId))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesWorkspaceIdIfNoProjectWasSelected()
            {
                var oldWorkspaceId = 11;
                var newWorkspaceId = 21;
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(10);
                timeEntry.WorkspaceId.Returns(oldWorkspaceId);
                timeEntry.ProjectId.Returns(12);
                InteractorFactory.GetTimeEntryById(Arg.Is(timeEntry.Id))
                    .Execute()
                    .Returns(Observable.Return(timeEntry));
                ViewModel.Prepare(timeEntry.Id);
                await ViewModel.Initialize();
                NavigationService.Navigate<SelectProjectViewModel, SelectProjectParameter, SelectProjectParameter>(
                        Arg.Any<SelectProjectParameter>())
                    .Returns(SelectProjectParameter.WithIds(null, null, newWorkspaceId));
                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.SaveCommand.Execute();

                await InteractorFactory
                    .Received()
                    .UpdateTimeEntry(Arg.Is<EditTimeEntryDto>(dto => dto.WorkspaceId == newWorkspaceId))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task InvertsTheFlagIfDescriptionWasBeingEdited()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(1);
                InteractorFactory.GetTimeEntryById(Arg.Is(timeEntry.Id))
                    .Execute()
                    .Returns(Observable.Return(timeEntry));
                ViewModel.Prepare(timeEntry.Id);
                await ViewModel.Initialize();

                ViewModel.SaveCommand.Execute();

                ViewModel.IsEditingDescription.Should().Be(false);
            }

            [Theory, LogIfTooSlow]
            [InlineData(null)]
            [InlineData(" ")]
            [InlineData("\t")]
            [InlineData("\n")]
            [InlineData("               ")]
            [InlineData("      \t  \n     ")]
            public async Task ReducesDescriptionConsistingOfOnlyEmptyCharactersToAnEmptyString(string description)
            {
                ViewModel.Description = description;

                ViewModel.SaveCommand.Execute();

                await InteractorFactory
                    .Received()
                    .UpdateTimeEntry(Arg.Is<EditTimeEntryDto>(dto => dto.Description.Length == 0))
                    .Execute();
            }

            [Theory, LogIfTooSlow]
            [InlineData(null, "")]
            [InlineData("   abcde", "abcde")]
            [InlineData("abcde     ", "abcde")]
            [InlineData("  abcde ", "abcde")]
            [InlineData("abcde  fgh", "abcde  fgh")]
            [InlineData("      abcd\nefgh     ", "abcd\nefgh")]
            public async Task TrimsDescriptionFromTheStartAndTheEndBeforeSaving(string description, string trimmed)
            {
                ViewModel.Description = description;

                ViewModel.SaveCommand.Execute();

                await InteractorFactory
                    .Received()
                    .UpdateTimeEntry(Arg.Is<EditTimeEntryDto>(dto => dto.Description == trimmed))
                    .Execute();
            }
        }

        public sealed class TheConfirmCommand : EditTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task DidNotCallUpdateIfDescriptionWasBeingEdited()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(1);
                InteractorFactory.GetTimeEntryById(Arg.Is(timeEntry.Id))
                    .Execute()
                    .Returns(Observable.Return(timeEntry));
                ViewModel.Prepare(timeEntry.Id);
                await ViewModel.Initialize();

                ViewModel.IsEditingDescription = true;
                ViewModel.ConfirmCommand.Execute();

                ViewModel.IsEditingDescription.Should().Be(false);

                await InteractorFactory
                    .DidNotReceive()
                    .UpdateTimeEntry(Arg.Any<EditTimeEntryDto>())
                    .Execute();
            }
        }

        public sealed class TheToggleBillableCommand : EditTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void TracksBillableTap()
            {
                ViewModel.ToggleBillableCommand.Execute();

                AnalyticsService.Received()
                                .EditViewTapped
                                .Track(Arg.Is(EditViewTapSource.Billable));
            }
        }

        public sealed class TheStartEditingDescriptionCommand : EditTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void TracksDescriptionTap()
            {
                ViewModel.StartEditingDescriptionCommand.Execute();

                AnalyticsService.Received()
                     .EditViewTapped
                     .Track(Arg.Is(EditViewTapSource.Description));
            }
        }

        public sealed class TheSelectTagsCommand : EditTimeEntryViewModelTest
        {
            [Property]
            public void NavigatesToTheSelectTagsViewModelPassingCurrentTagIds(NonNegativeInt[] nonNegativeInts)
            {
                var tagIds = nonNegativeInts.Select(i => (long)i.Get)
                    .Distinct();
                long id = 13;
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(id);
                timeEntry.TagIds.Returns(tagIds);
                InteractorFactory.GetTimeEntryById(Arg.Is(id))
                    .Execute()
                    .Returns(Observable.Return(timeEntry));
                ViewModel.Prepare(id);
                ViewModel.Initialize().Wait();

                ViewModel.SelectTagsCommand.ExecuteAsync().Wait();

                NavigationService
                    .Received()
                    .Navigate<SelectTagsViewModel, (long[] tagIds, long workspaceId), long[]>(
                        Arg.Is<(long[] tagIds, long workspaceId)>(
                            tuple => tuple.tagIds.SequenceEqual(tagIds)))
                    .Wait();
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheSelectTagsViewModelPassingWorkspaceId()
            {
                long workspaceId = 13;
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                workspace.Id.Returns(workspaceId);
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(14);
                timeEntry.WorkspaceId.Returns(workspaceId);
                InteractorFactory.GetTimeEntryById(Arg.Any<long>())
                    .Execute()
                    .Returns(Observable.Return(timeEntry));
                ViewModel.Prepare(timeEntry.Id);
                await ViewModel.Initialize();

                await ViewModel.SelectTagsCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Navigate<SelectTagsViewModel, (long[] tagIds, long workspaceId), long[]>(
                        Arg.Is<(long[] tagIds, long workspaceId)>(tuple => tuple.workspaceId == workspaceId)
                    );
            }

            [Property]
            public void QueriesTheDataSourceForReturnedTagIds(
                NonEmptyArray<NonNegativeInt> nonNegativeInts)
            {
                var tagIds = nonNegativeInts.Get
                    .Select(i => (long)i.Get)
                    .ToArray();
                var tags = tagIds.Select(createTag);
                DataSource.Tags.GetAll(Arg.Any<Func<IDatabaseTag, bool>>())
                    .Returns(Observable.Return(tags));
                NavigationService
                    .Navigate<SelectTagsViewModel, (long[], long), long[]>(Arg.Any<(long[], long)>())
                    .Returns(Task.FromResult(tagIds));
                ViewModel.Initialize().Wait();

                ViewModel.SelectTagsCommand.ExecuteAsync().Wait();

                DataSource.Tags.Received()
                    .GetAll(Arg.Is<Func<IDatabaseTag, bool>>(
                        func => tags.All(func)))
                    .Wait();
            }

            [Property]
            public Property SetsTheReturnedTags()
            {
                return Prop.ForAll(Arb.Default.NonEmptyArray<NonNegativeInt>(), nonNegativeInts =>
                {
                    var viewModel = CreateViewModel();

                    var tagIds = nonNegativeInts.Get
                        .Select(i => (long)i.Get)
                        .ToArray();
                    var tags = tagIds.Select(createTag);
                    var tagNames = new HashSet<string>(tags.Select(tag => tag.Name));
                    viewModel.Initialize().Wait();
                    DataSource.Tags.GetAll(Arg.Any<Func<IDatabaseTag, bool>>())
                        .Returns(Observable.Return(tags));
                    NavigationService
                        .Navigate<SelectTagsViewModel, (long[], long), long[]>(Arg.Any<(long[], long)>())
                        .Returns(Task.FromResult(tagIds));

                    viewModel.SelectTagsCommand.ExecuteAsync().Wait();

                    viewModel.Tags.Should()
                             .HaveCount(tags.Count()).And
                             .OnlyContain(tag => tagNames.Contains(tag));
                });
            }

            [Fact, LogIfTooSlow]
            public async Task TracksTagSelectorOpens()
            {
                await ViewModel.SelectTagsCommand.ExecuteAsync();

                AnalyticsService.Received().EditEntrySelectTag.Track();
                AnalyticsService.Received()
                                .EditViewTapped
                                .Track(Arg.Is(EditViewTapSource.Tags));
            }

            private IThreadSafeTag createTag(long id)
            {
                var tag = Substitute.For<IThreadSafeTag>();
                tag.Id.Returns(id);
                tag.Name.Returns($"Tag{id}");
                return tag;
            }
        }

        public sealed class TheDismissSyncErrorMessageCommand : EditTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async Task SetsSyncErrorMessageVisiblePropertyToFalse(bool initialValue)
            {
                var errorMessage = initialValue ? "Some error" : null;
                var id = 13;
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(id);
                timeEntry.LastSyncErrorMessage.Returns(errorMessage);
                InteractorFactory.GetTimeEntryById(Arg.Is<long>(id))
                    .Execute()
                    .Returns(Observable.Return(timeEntry));
                ViewModel.Prepare(id);
                await ViewModel.Initialize();

                ViewModel.DismissSyncErrorMessageCommand.Execute();

                ViewModel.SyncErrorMessageVisible.Should().BeFalse();
            }
        }

        public sealed class TheInitializeMethod : EditTimeEntryViewModelTest
        {
            private readonly IThreadSafeTimeEntry timeEntry;

            public TheInitializeMethod()
            {
                timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(Id);
                InteractorFactory.GetTimeEntryById(Arg.Is<long>(Id))
                    .Execute()
                    .Returns(Observable.Return(timeEntry));
            }

            [Property]
            public void SetsTheSyncErrorMessageProperty(string errorMessage)
            {
                timeEntry.LastSyncErrorMessage.Returns(errorMessage);
                ViewModel.Prepare(Id);

                ViewModel.Initialize().Wait();

                ViewModel.SyncErrorMessage.Should().Be(errorMessage);
            }

            [Fact, LogIfTooSlow]
            public void SetsTheSyncErrorMessageWhenTheTimeEntryIsInaccessible()
            {
                timeEntry.IsInaccessible.Returns(true);
                timeEntry.LastSyncErrorMessage.Returns("Some less important error message");

                ViewModel.Prepare(Id);

                ViewModel.Initialize().Wait();

                ViewModel.SyncErrorMessage.Should().Be(Resources.InaccessibleTimeEntryErrorMessage);
            }

            [Theory, LogIfTooSlow]
            [InlineData("Some error", true)]
            [InlineData("", false)]
            [InlineData(null, false)]
            public async Task SetsTheSyncErrorMessageVisibleProperty(
                string errorMessage, bool expectedVisibility)
            {
                timeEntry.LastSyncErrorMessage.Returns(errorMessage);
                ViewModel.Prepare(Id);

                await ViewModel.Initialize();

                ViewModel.SyncErrorMessageVisible.Should().Be(expectedVisibility);
            }

            [Property]
            public void CopiesAllInformationFromTheEditedTimeEntrySoNothingIsLost(
                long id,
                long workspaceId,
                long? projectId,
                long? taskId,
                bool billable,
                DateTimeOffset start,
                long? duration,
                NonNull<string> description,
                NonNull<long[]> tagIds)
            {
                var viewModel = CreateViewModel(); // view model must be created for each run of the property test
                DataSource.TimeEntries.ClearReceivedCalls();

                var uniqueTagIds = tagIds.Get.Distinct().ToArray();
                if (projectId == null)
                    taskId = null;
                var mockedTimeEntry = mockTimeEntry(id, workspaceId, projectId, taskId, billable, start, duration,
                    description.Get, uniqueTagIds);
                var observable = Observable.Return(mockedTimeEntry);
                InteractorFactory.GetTimeEntryById(id)
                    .Execute()
                    .Returns(observable);

                viewModel.Prepare(id);
                viewModel.Initialize().Wait();
                viewModel.IsEditingDescription = false;
                viewModel.ConfirmCommand.Execute();

                InteractorFactory
                    .Received()
                    .UpdateTimeEntry(Arg.Is<EditTimeEntryDto>(
                        dto => dto.Id == id
                            && dto.WorkspaceId == workspaceId
                            && dto.ProjectId == projectId
                            && dto.TaskId == taskId
                            && dto.Billable == billable
                            && dto.StartTime == start
                            && dto.StopTime == (duration.HasValue ? start + TimeSpan.FromSeconds(duration.Value) : (DateTimeOffset?)null)
                            && dto.Description == description.Get.Trim()
                            && dto.TagIds.Count() == uniqueTagIds.Count()
                            && dto.TagIds.All(tagId => uniqueTagIds.Any(originalTagId => originalTagId == tagId))))
                    .Execute()
                    .Wait();
            }

            private IThreadSafeTimeEntry mockTimeEntry(
                long id,
                long workspaceId,
                long? projectId,
                long? taskId,
                bool billable,
                DateTimeOffset start,
                long? duration,
                string description,
                long[] tagIds)
            {
                var databaseTimeEntry = Substitute.For<IThreadSafeTimeEntry>();

                databaseTimeEntry.Id.Returns(id);
                databaseTimeEntry.WorkspaceId.Returns(workspaceId);

                IThreadSafeProject project = null;
                if (projectId.HasValue)
                {
                    project = Substitute.For<IThreadSafeProject>();
                    project.Id.Returns(projectId.Value);
                }
                databaseTimeEntry.Project.Returns(project);

                IThreadSafeTask task = null;
                if (taskId.HasValue)
                {
                    task = Substitute.For<IThreadSafeTask>();
                    task.Id.Returns(taskId.Value);
                }
                databaseTimeEntry.Task.Returns(task);

                databaseTimeEntry.Billable.Returns(billable);
                databaseTimeEntry.Start.Returns(start);
                databaseTimeEntry.Duration.Returns(duration);
                databaseTimeEntry.Description.Returns(description);
                databaseTimeEntry.TagIds.Returns(tagIds);

                return databaseTimeEntry;
            }
        }

        public sealed class TheSelectProjectCommand : EditTimeEntryViewModelTest
        {
            private async Task prepare(
                long? projectId = null,
                string projectName = null,
                string projectColor = null,
                string clientName = null,
                long? taskId = null,
                string taskName = null,
                bool active = true)
            {
                long timeEntryId = 10;
                prepareTimeEntry(timeEntryId);

                if (projectId.HasValue)
                    prepareProject(projectId.Value, projectName, projectColor, clientName, 0);

                if (taskId.HasValue)
                    prepareTask(taskId.Value, taskName);

                prepareNavigationService(projectId, taskId);

                ViewModel.Prepare(timeEntryId);
                await ViewModel.Initialize();
            }

            private IThreadSafeTimeEntry prepareTimeEntry(long id)
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Id.Returns(id);
                timeEntry.Description.Returns("Doing stuff");
                timeEntry.Project.Name.Returns(Guid.NewGuid().ToString());
                timeEntry.Project.Color.Returns(Guid.NewGuid().ToString());
                timeEntry.Task.Name.Returns(Guid.NewGuid().ToString());
                timeEntry.Project.Client.Name.Returns(Guid.NewGuid().ToString());
                InteractorFactory.GetTimeEntryById(Arg.Is(id))
                    .Execute()
                    .Returns(Observable.Return(timeEntry));
                return timeEntry;
            }

            private IThreadSafeProject prepareProject(
                long projectId, string projectName, string projectColor, string clientName, long workspaceId, bool active = true)
            {
                var project = Substitute.For<IThreadSafeProject>();
                project.Id.Returns(projectId);
                project.Name.Returns(projectName);
                project.Color.Returns(projectColor);
                project.Client.Name.Returns(clientName);
                project.WorkspaceId.Returns(workspaceId);
                project.Active.Returns(active);
                InteractorFactory.GetProjectById(Arg.Is(projectId))
                    .Execute()
                    .Returns(Observable.Return(project));
                return project;
            }

            private void prepareTask(long taskId, string taskName)
            {
                var task = Substitute.For<IThreadSafeTask>();
                task.Id.Returns(taskId);
                task.Name.Returns(taskName);
                InteractorFactory.GetTaskById(Arg.Is(task.Id))
                    .Execute()
                    .Returns(Observable.Return(task));
            }

            private void prepareNavigationService(long? projectId, long? taskId)
                => NavigationService
                    .Navigate<SelectProjectViewModel, SelectProjectParameter, SelectProjectParameter>(
                           Arg.Any<SelectProjectParameter>())
                       .Returns(SelectProjectParameter.WithIds(projectId, taskId, 0));

            private List<IThreadSafeTag> createTags(int count)
                => Enumerable.Range(10000, count)
                    .Select(i =>
                    {
                        var tag = Substitute.For<IThreadSafeTag>();
                        tag.Name.Returns($"Tag{i}");
                        tag.Id.Returns(i);
                        return tag;
                    }).ToList();

            [Fact, LogIfTooSlow]
            public async Task SetsTheOnboardingStorageFlag()
            {
                var projectName = "Some other project";
                await prepare(projectId: 11, projectName: projectName);

                await ViewModel.SelectProjectCommand.ExecuteAsync();

                OnboardingStorage.Received().SelectsProject();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheProject()
            {
                var projectName = "Some other project";
                await prepare(projectId: 11, projectName: projectName);

                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.Project.Should().Be(projectName);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheTask()
            {
                var taskName = "Some task";
                await prepare(
                    projectId: 11,
                    projectName: "Project",
                    taskId: 12,
                    taskName: taskName);

                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.Task.Should().Be(taskName);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheClient()
            {
                var clientName = "Some client";
                await prepare(
                    projectId: 11,
                    projectName: "Project",
                    clientName: clientName);

                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.Client.Should().Be(clientName);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheColor()
            {
                var projectColor = "123456";
                await prepare(
                    projectId: 11,
                    projectName: "Project",
                    projectColor: projectColor);

                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.ProjectColor.Should().Be(projectColor);
            }

            [Fact, LogIfTooSlow]
            public async Task RemovesTheTaskIfNoTaskWasSelected()
            {
                await prepare(11, "Some project");

                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.Task.Should().BeEmpty();
            }

            [Fact, LogIfTooSlow]
            public async Task RemovesTagsIfProjectFromAnotherWorkspaceWasSelected()
            {
                var initialTagCount = 10;
                long timeEntryId = 10;
                long initialProjectId = 11;
                long newProjectId = 12;
                prepareProject(initialProjectId, "Initial project", "#123456", "Some client", 13);
                prepareProject(newProjectId, "New project", "AABBCC", "Some client", 14);
                prepareNavigationService(newProjectId, null);
                var timeEntry = prepareTimeEntry(timeEntryId);
                var tags = createTags(initialTagCount);
                timeEntry.Tags.Returns(tags);
                ViewModel.Prepare(timeEntryId);
                await ViewModel.Initialize();
                ViewModel.Tags.Should().HaveCount(initialTagCount);

                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.Tags.Should().HaveCount(0);
            }

            [Fact, LogIfTooSlow]
            public async Task TracksProjectSelectorOpens()
            {
                await prepare(11, "Some project");

                await ViewModel.SelectProjectCommand.ExecuteAsync();

                AnalyticsService.Received().EditEntrySelectProject.Track();
                AnalyticsService.Received()
                                .EditViewTapped
                                .Track(Arg.Is(EditViewTapSource.Project));
            }
        }

        public sealed class TheTagsProperty : EditTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(31, "a")]
            [InlineData(31, "💵")]
            [InlineData(50, "b")]
            [InlineData(50, "🚕")]
            public async Task CutsLongTagNames(int tagLength, string tagGrapheme)
            {
                await prepareTest(tagLength, tagGrapheme);

                ViewModel.Tags.Should()
                    .OnlyContain(tag => tag.LengthInGraphemes() == 33 && tag.EndsWith("..."));
            }

            [Theory, LogIfTooSlow]
            [InlineData(30, "a")]
            [InlineData(30, "🐕")]
            [InlineData(29, "b")]
            [InlineData(29, "🐛")]
            [InlineData(10, "c")]
            [InlineData(10, "❄️")]
            public async Task DoesNotCutShortTagNames(int tagLength, string tagGrapheme)
            {
                await prepareTest(tagLength, tagGrapheme);

                ViewModel.Tags.Should().OnlyContain(tag => tag.LengthInGraphemes() == tagLength);
            }

            private async Task prepareTest(int tagLength, string tagGrapheme)
            {
                var tag = Substitute.For<IThreadSafeTag>();
                tag.Name.Returns(getLongTagName(tagLength, tagGrapheme));
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();

                timeEntry.Id.Returns(13);
                timeEntry.Tags.Returns(new IThreadSafeTag[] { tag });

                InteractorFactory.GetTimeEntryById(Arg.Is(timeEntry.Id))
                    .Execute()
                    .Returns(Observable.Return(timeEntry));

                ViewModel.Prepare(timeEntry.Id);
                await ViewModel.Initialize();
            }

            private string getLongTagName(int length, string tagGrapheme)
                => Enumerable
                    .Range(0, length)
                    .Aggregate(new StringBuilder(), (builder, _) => builder.Append(tagGrapheme))
                    .ToString();
        }

        public sealed class TheDescriptionLimitExceededProperty : EditTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("a", MaxTimeEntryDescriptionLengthInBytes - 1)]
            [InlineData("c", MaxTimeEntryDescriptionLengthInBytes)]
            [InlineData("ॷ", MaxTimeEntryDescriptionLengthInBytes / 3)] //This symbol is 3 bytes long
            [InlineData("Љ", MaxTimeEntryDescriptionLengthInBytes / 2)] //This symbol is 2 bytes long
            public void IsFalseWhenDescriptionIsShorterThanMaxLimit(
                string character, int count)
            {
                ViewModel.Description = createLongString(character, count);

                ViewModel.DescriptionLimitExceeded.Should().BeFalse();
            }

            [Theory, LogIfTooSlow]
            [InlineData("A", MaxTimeEntryDescriptionLengthInBytes + 1)]
            [InlineData("ॷ", MaxTimeEntryDescriptionLengthInBytes / 3 + 1)] //This symbol is 3 bytes long
            [InlineData("Љ", MaxTimeEntryDescriptionLengthInBytes / 2 + 1)] //This symbol is 2 bytes long
            public void IsTrueWhenDescriptionIsLongerThanMaxLimit(
                string character, int count)
            {
                ViewModel.Description = createLongString(character, count);

                ViewModel.DescriptionLimitExceeded.Should().BeTrue();
            }

            private string createLongString(string character, int count)
                => Enumerable
                    .Range(0, count)
                    .Aggregate(
                        new StringBuilder(),
                        (builder, _) => builder.Append(character))
                    .ToString();
        }

        public sealed class TheSelectStartDateCommand : EditTimeEntryViewModelTest
        {
            [Fact]
            public async Task OpensTheSelectDateTimeViewModel()
            {
                ConfigureEditedTimeEntry(DateTimeOffset.UtcNow, false);
                ViewModel.Prepare(Id);
                ViewModel.Initialize().Wait();

                await ViewModel.SelectStartDateCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(
                        Arg.Any<DateTimePickerParameters>());
            }

            [Fact]
            public async Task OpensTheSelectDateTimeViewModelWithCorrectLimitsForARunningTimeEntry()
            {
                var now = DateTimeOffset.UtcNow;
                ConfigureEditedTimeEntry(now, true);
                ViewModel.Prepare(Id);
                ViewModel.Initialize().Wait();

                await ViewModel.SelectStartDateCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(
                        Arg.Is<DateTimePickerParameters>(param => param.MinDate == now - MaxTimeEntryDuration && param.MaxDate == now));
            }

            [Fact]
            public async Task OpensTheSelectDateTimeViewModelWithCorrectLimitsForAStoppedTimeEntry()
            {
                var now = DateTimeOffset.UtcNow;
                ConfigureEditedTimeEntry(now, false);
                ViewModel.Prepare(Id);
                ViewModel.Initialize().Wait();

                await ViewModel.SelectStartDateCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(
                        Arg.Is<DateTimePickerParameters>(param => param.MinDate == EarliestAllowedStartTime && param.MaxDate == LatestAllowedStartTime));
            }

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ChangesTheStartTimeToTheSelectedStartDate(bool isRunning)
            {
                var now = DateTimeOffset.UtcNow;
                var startTime = now.AddMonths(-1);
                ConfigureEditedTimeEntry(now, isRunning);
                ViewModel.Prepare(Id);
                ViewModel.Initialize().Wait();
                NavigationService
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(Arg.Any<DateTimePickerParameters>())
                    .Returns(startTime);

                await ViewModel.SelectStartDateCommand.ExecuteAsync();

                TheTimeEntry.Start.Should().NotBe(startTime);
            }

            [Fact]
            public async Task DoesNotChangeDurationForAStoppedTimeEntry()
            {
                var now = DateTimeOffset.UtcNow;
                ConfigureEditedTimeEntry(now, false);
                ViewModel.Prepare(Id);
                ViewModel.Initialize().Wait();
                var duration = Duration;

                await ViewModel.SelectStartDateCommand.ExecuteAsync();

                TheTimeEntry.Duration.Should().Be((long)duration.TotalSeconds);
            }

            [Fact, LogIfTooSlow]
            public async Task TracksStartDateTap()
            {
                ConfigureEditedTimeEntry(DateTimeOffset.Now);
                ViewModel.Prepare(Id);
                ViewModel.Initialize().Wait();

                var newStartTime = TheTimeEntry.Start.AddHours(1);
                NavigationService
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(
                        Arg.Any<DateTimePickerParameters>())
                    .Returns(newStartTime);

                await ViewModel.SelectStartDateCommand.ExecuteAsync();

                AnalyticsService.Received()
                                .EditViewTapped
                                .Track(Arg.Is(EditViewTapSource.StartDate));
            }
        }

        public sealed class TheSelectStartTimeCommand : EditTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task TracksStartTimeTap()
            {
                ConfigureEditedTimeEntry(DateTimeOffset.Now);
                ViewModel.Prepare(Id);
                ViewModel.Initialize().Wait();

                var newStartTime = TheTimeEntry.Start.AddHours(1);
                var newDurationParameter = DurationParameter.WithStartAndDuration(newStartTime, null);
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(
                        Arg.Any<EditDurationParameters>())
                    .Returns(newDurationParameter);

                await ViewModel.SelectStartTimeCommand.ExecuteAsync();

                AnalyticsService.Received()
                                .EditViewTapped
                                .Track(Arg.Is(EditViewTapSource.StartTime));
            }
        }

        public sealed class TheSelectStopTimeCommand : EditTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task TracksStopTimeTap()
            {
                ConfigureEditedTimeEntry(DateTimeOffset.Now);
                ViewModel.Prepare(Id);
                ViewModel.Initialize().Wait();

                var newStartTime = TheTimeEntry.Start.AddHours(1);
                var newDurationParameter = DurationParameter.WithStartAndDuration(newStartTime, null);
                NavigationService
                    .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(
                        Arg.Any<EditDurationParameters>())
                    .Returns(newDurationParameter);

                await ViewModel.SelectStopTimeCommand.ExecuteAsync();

                AnalyticsService.Received()
                                .EditViewTapped
                                .Track(Arg.Is(EditViewTapSource.StopTime));
            }
        }

        public sealed class TheHasProjectProperty : EditTimeEntryViewModelTest
        {
            [Fact]
            public void EmitsTrueWhenTimeEntryHasProject()
            {
                const string selectedProject = "Some random project";

                bool actualHasProjectValue = false;
                ViewModel.HasProject.Subscribe(hasProject => actualHasProjectValue = hasProject);
                ViewModel.Project = selectedProject;

                actualHasProjectValue.Should().BeTrue();
            }

            [Fact]
            public void EmitsFalseWhenTimeEntryDoesNotHaveProject()
            {
                const string selectedProject = "Some random project";

                ViewModel.Project = selectedProject;
                bool actualHasProjectValue = true;
                ViewModel.HasProject.Subscribe(hasProject => actualHasProjectValue = hasProject);
                ViewModel.Project = null;

                actualHasProjectValue.Should().BeFalse();
            }
        }
    }
}
