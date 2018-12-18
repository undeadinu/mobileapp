using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Tests.Generators;
using Xunit;
using TimeEntry = Toggl.Foundation.Models.TimeEntry;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.DataSources;
using System.Reactive.Subjects;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SuggestionsViewModelTests
    {
        public abstract class SuggestionsViewModelTest : BaseViewModelTests<SuggestionsViewModel>
        {
            protected override SuggestionsViewModel CreateViewModel()
                => new SuggestionsViewModel(DataSource, InteractorFactory, OnboardingStorage, SuggestionProviderContainer, SchedulerProvider, RxActionFactory);

            protected override void AdditionalViewModelSetup()
            {
                base.AdditionalViewModelSetup();

                var provider = Substitute.For<ISuggestionProvider>();
                provider.GetSuggestions().Returns(Observable.Empty<Suggestion>());
                SuggestionProviderContainer.Providers.Returns(new[] { provider }.ToList().AsReadOnly());
            }

            protected void SetProviders(ISuggestionProviderContainer container, params ISuggestionProvider[] providers)
            {
                container.Providers.Returns(providers.ToList().AsReadOnly());
            }
        }

        public sealed class TheConstructor : SuggestionsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useContainer,
                bool useOnboardingStorage,
                bool useInteractorFactory,
                bool useSchedulerProvider,
                bool useRxActionFactory)
            {
                var container = useContainer ? SuggestionProviderContainer : null;
                var dataSource = useDataSource ? DataSource : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SuggestionsViewModel(dataSource, interactorFactory, onboardingStorage, container,
                        schedulerProvider, rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheSuggestionsProperty : SuggestionsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task WorksWithSeveralProviders()
            {
                var provider1 = Substitute.For<ISuggestionProvider>();
                var provider2 = Substitute.For<ISuggestionProvider>();
                var suggestion1 = createSuggestion("t1", 12, 9);
                var suggestion2 = createSuggestion("t2", 9, 12);
                provider1.GetSuggestions().Returns(Observable.Return(suggestion1));
                provider2.GetSuggestions().Returns(Observable.Return(suggestion2));
                SetProviders(SuggestionProviderContainer, provider1, provider2);
                var observer = TestScheduler.CreateObserver<Suggestion[]>();

                await ViewModel.Initialize();
                ViewModel.Suggestions.Subscribe(observer);
                TestScheduler.Start();

                var suggestions = observer.Messages.First().Value.Value;
                suggestions.Should().HaveCount(2).And.Contain(new[] { suggestion1, suggestion2 });
            }

            [Fact, LogIfTooSlow]
            public async Task WorksIfProviderHasMultipleSuggestions()
            {
                var provider = Substitute.For<ISuggestionProvider>();
                var suggestions = Enumerable.Range(1, 3).Select(createSuggestion).ToArray();
                var observableContent = suggestions
                    .Select(suggestion => createRecorded(1, suggestion))
                    .ToArray();
                var observable = TestScheduler.CreateColdObservable(observableContent).Take(suggestions.Length);
                provider.GetSuggestions().Returns(observable);
                SetProviders(SuggestionProviderContainer, provider);
                var observer = TestScheduler.CreateObserver<Suggestion[]>();

                await ViewModel.Initialize();
                ViewModel.Suggestions.Subscribe(observer);
                TestScheduler.Start();

                var receivedSuggestions = observer.Messages.First().Value.Value;
                receivedSuggestions.Should().HaveCount(suggestions.Length).And.Contain(suggestions);
            }

            [Fact, LogIfTooSlow]
            public async Task WorksIfProvidersAreEmpty()
            {
                var providers = Enumerable.Range(0, 3)
                    .Select(_ => Substitute.For<ISuggestionProvider>()).ToArray();

                foreach (var provider in providers)
                    provider.GetSuggestions().Returns(Observable.Empty<Suggestion>());

                SetProviders(SuggestionProviderContainer, providers);
                var observer = TestScheduler.CreateObserver<Suggestion[]>();

                await ViewModel.Initialize();
                ViewModel.Suggestions.Subscribe(observer);
                TestScheduler.Start();

                var suggestions = observer.Messages.First().Value.Value;
                suggestions.Should().HaveCount(0);
            }

            [Fact, LogIfTooSlow]
            public async Task ReloadsSuggestionsWhenWorkspacesUpdate()
            {
                var workspaceUpdatedSubject = new Subject<Unit>();
                InteractorFactory.ObserveWorkspaceOrTimeEntriesChanges().Execute()
                    .Returns(workspaceUpdatedSubject.AsObservable());

                var provider = suggestionProvider();
                SetProviders(SuggestionProviderContainer, provider);
                var observer = TestScheduler.CreateObserver<Suggestion[]>();

                await ViewModel.Initialize();
                ViewModel.Suggestions.Subscribe(observer);

                workspaceUpdatedSubject.OnNext(Unit.Default);

                TestScheduler.Start();

                observer.Messages.Should().HaveCount(2);
                observer.Messages.First().Value.Value.Should().HaveCount(0);
                observer.Messages.Last().Value.Value.Should().HaveCount(0);
                await provider.Received(2).GetSuggestions();
            }

            [Fact, LogIfTooSlow]
            public async Task ReloadsSuggestionsWhenTimeEntriesUpdate()
            {
                var changesSubject = new Subject<Unit>();
                InteractorFactory.ObserveWorkspaceOrTimeEntriesChanges().Execute().Returns(changesSubject);

                var provider = suggestionProvider();
                SetProviders(SuggestionProviderContainer, provider);
                var observer = TestScheduler.CreateObserver<Suggestion[]>();

                await ViewModel.Initialize();
                ViewModel.Suggestions.Subscribe(observer);

                changesSubject.OnNext(Unit.Default);

                TestScheduler.Start();

                observer.Messages.Should().HaveCount(2);
                observer.Messages.First().Value.Value.Should().HaveCount(0);
                observer.Messages.Last().Value.Value.Should().HaveCount(0);
                await provider.Received(2).GetSuggestions();
            }

            private ISuggestionProvider suggestionProvider()
            {
                var provider = Substitute.For<ISuggestionProvider>();

                provider.GetSuggestions().Returns(Observable.Empty<Suggestion>());

                return provider;
            }

            private Suggestion createSuggestion(int index) => createSuggestion($"te{index}", 0, 0);

            private Suggestion createSuggestion(string description, long taskId, long projectId) => new Suggestion(
                TimeEntry.Builder.Create(0)
                    .SetDescription(description)
                    .SetStart(DateTimeOffset.UtcNow)
                    .SetAt(DateTimeOffset.UtcNow)
                    .SetTaskId(taskId)
                    .SetProjectId(projectId)
                    .SetWorkspaceId(11)
                    .SetUserId(12)
                    .Build()
            );

            private Recorded<Notification<Suggestion>> createRecorded(int ticks, Suggestion suggestion)
                => new Recorded<Notification<Suggestion>>(ticks, Notification.CreateOnNext(suggestion));
        }

        public sealed class TheStartTimeEntryAction : SuggestionsViewModelTest
        {
            public TheStartTimeEntryAction()
            {
                var user = Substitute.For<IThreadSafeUser>();
                user.Id.Returns(10);
                DataSource.User.Current.Returns(Observable.Return(user));

                TimeService.CurrentDateTime.Returns(DateTimeOffset.Now);
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheCreateTimeEntryInteractor()
            {
                var suggestion = createSuggestion();
                await ViewModel.Initialize();

                ViewModel.StartTimeEntry.Execute(suggestion);
                TestScheduler.Start();

                InteractorFactory.Received().StartSuggestion(suggestion);
            }

            [Fact, LogIfTooSlow]
            public async Task ExecutesTheContinueTimeEntryInteractor()
            {
                var suggestion = createSuggestion();
                var mockedInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeTimeEntry>>>();
                InteractorFactory.StartSuggestion(Arg.Any<Suggestion>()).Returns(mockedInteractor);
                await ViewModel.Initialize();

                ViewModel.StartTimeEntry.Execute(suggestion);
                TestScheduler.Start();

                await mockedInteractor.Received().Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task CanBeExecutedForTheSecondTimeIfStartingTheFirstOneFinishesSuccessfully()
            {
                var suggestion = createSuggestion();
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                var mockedInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeTimeEntry>>>();
                InteractorFactory.StartSuggestion(Arg.Any<Suggestion>()).Returns(mockedInteractor);
                mockedInteractor.Execute()
                    .Returns(Observable.Return(timeEntry));
                await ViewModel.Initialize();

                var auxObservable = TestScheduler.CreateObserver<Unit>();
                Observable.Concat(
                        Observable.Defer(() => ViewModel.StartTimeEntry.Execute(suggestion)),
                        Observable.Defer(() => ViewModel.StartTimeEntry.Execute(suggestion))
                    )
                    .Subscribe(auxObservable);
                TestScheduler.Start();

                InteractorFactory.Received(2).StartSuggestion(suggestion);
            }

            [Fact, LogIfTooSlow]
            public async Task MarksTheActionForOnboardingPurposes()
            {
                var suggestion = createSuggestion();
                await ViewModel.Initialize();

                var auxObservable = TestScheduler.CreateObserver<Unit>();
                Observable.Concat(
                        Observable.Defer(() => ViewModel.StartTimeEntry.Execute(suggestion)),
                        Observable.Defer(() => ViewModel.StartTimeEntry.Execute(suggestion))
                    )
                    .Subscribe(auxObservable);
                TestScheduler.Start();

                OnboardingStorage.Received().SetTimeEntryContinued();
            }

            private Suggestion createSuggestion()
            {
                var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                timeEntry.Duration.Returns((long)TimeSpan.FromMinutes(30).TotalSeconds);
                timeEntry.Description.Returns("Testing");
                timeEntry.WorkspaceId.Returns(10);
                return new Suggestion(timeEntry);
            }
        }
    }
}
