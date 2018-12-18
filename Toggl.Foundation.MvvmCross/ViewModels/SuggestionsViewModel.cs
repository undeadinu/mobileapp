using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Suggestions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Services;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SuggestionsViewModel : MvxViewModel
    {
        private readonly IInteractorFactory interactorFactory;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly ISuggestionProviderContainer suggestionProviders;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly ITogglDataSource dataSource;
        private readonly IRxActionFactory rxActionFactory;

        public IObservable<Suggestion[]> Suggestions { get; private set; }
        public IObservable<bool> IsEmpty { get; private set; }
        public InputAction<Suggestion> StartTimeEntry { get; private set; }

        public SuggestionsViewModel(
            ITogglDataSource dataSource,
            IInteractorFactory interactorFactory,
            IOnboardingStorage onboardingStorage,
            ISuggestionProviderContainer suggestionProviders,
            ISchedulerProvider schedulerProvider,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(suggestionProviders, nameof(suggestionProviders));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.interactorFactory = interactorFactory;
            this.onboardingStorage = onboardingStorage;
            this.suggestionProviders = suggestionProviders;
            this.schedulerProvider = schedulerProvider;
            this.dataSource = dataSource;
            this.rxActionFactory = rxActionFactory;
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            StartTimeEntry = rxActionFactory.FromAsync<Suggestion>(suggestion => startTimeEntry(suggestion));

            Suggestions = interactorFactory.ObserveWorkspaceOrTimeEntriesChanges().Execute()
                .StartWith(Unit.Default)
                .SelectMany(_ => getSuggestions())
                .AsDriver(onErrorJustReturn: new Suggestion[0], schedulerProvider: schedulerProvider);

            IsEmpty = Suggestions
                .Select(suggestions => suggestions.Length == 0)
                .StartWith(true)
                .AsDriver(onErrorJustReturn: true, schedulerProvider: schedulerProvider);
        }

        private IObservable<Suggestion[]> getSuggestions()
        {
            return suggestionProviders
                .Providers
                .Select(provider => provider.GetSuggestions())
                .Aggregate(Observable.Merge)
                .ToArray();
        }

        private async Task startTimeEntry(Suggestion suggestion)
        {
            onboardingStorage.SetTimeEntryContinued();

            await interactorFactory
                .StartSuggestion(suggestion)
                .Execute();
        }
    }
}
