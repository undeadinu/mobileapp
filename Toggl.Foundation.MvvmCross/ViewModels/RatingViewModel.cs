using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels.Hints;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class RatingViewModel : MvxViewModel
    {
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IRatingService ratingService;
        private readonly IAnalyticsService analyticsService;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IMvxNavigationService navigationService;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IRxActionFactory rxActionFactory;

        private readonly BehaviorSubject<bool?> impressionSubject = new BehaviorSubject<bool?>(null);
        private readonly ISubject<bool> isFeedbackSuccessViewShowing = new Subject<bool>();

        private bool impressionWasRegistered => impressionSubject.Value != null;

        // Warning: this property will throw if no impression has been registered yet.
        private bool impressionIsPositive => impressionSubject.Value.Value;

        public IObservable<bool?> Impression { get; }

        public IObservable<string> CallToActionTitle { get; }

        public IObservable<string> CallToActionDescription { get; }

        public IObservable<string> CallToActionButtonTitle { get; }

        public IObservable<bool> IsFeedbackSuccessViewShowing { get; }

        public UIAction PerformMainAction { get; }

        public RatingViewModel(
            ITimeService timeService,
            ITogglDataSource dataSource,
            IRatingService ratingService,
            IAnalyticsService analyticsService,
            IOnboardingStorage onboardingStorage,
            IMvxNavigationService navigationService,
            ISchedulerProvider schedulerProvider,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(ratingService, nameof(ratingService));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.ratingService = ratingService;
            this.analyticsService = analyticsService;
            this.onboardingStorage = onboardingStorage;
            this.navigationService = navigationService;
            this.schedulerProvider = schedulerProvider;
            this.rxActionFactory = rxActionFactory;

            Impression = impressionSubject.AsDriver(this.schedulerProvider);

            CallToActionTitle = impressionSubject
                .Select(callToActionTitle)
                .AsDriver(this.schedulerProvider);

            CallToActionDescription = impressionSubject
                .Select(callToActionDescription)
                .AsDriver(this.schedulerProvider);

            CallToActionButtonTitle = impressionSubject
                .Select(callToActionButtonTitle)
                .AsDriver(this.schedulerProvider);

            IsFeedbackSuccessViewShowing = isFeedbackSuccessViewShowing.AsDriver(this.schedulerProvider);

            PerformMainAction = rxActionFactory.FromAsync(performMainAction);
        }

        public void CloseFeedbackSuccessView()
        {
            isFeedbackSuccessViewShowing.OnNext(false);
        }

        public void RegisterImpression(bool isPositive)
        {
            impressionSubject.OnNext(isPositive);
            analyticsService.UserFinishedRatingViewFirstStep.Track(isPositive);

            if (isPositive)
            {
                trackStepOutcome(
                    RatingViewOutcome.PositiveImpression,
                    analyticsService.RatingViewFirstStepLike);
            }
            else
            {
                trackStepOutcome(
                    RatingViewOutcome.NegativeImpression,
                    analyticsService.RatingViewFirstStepDislike);
            }
        }

        private string callToActionTitle(bool? impressionIsPositive)
        {
            if (impressionIsPositive == null)
                return string.Empty;

            return impressionIsPositive.Value
                   ? Resources.RatingViewPositiveCallToActionTitle
                   : Resources.RatingViewNegativeCallToActionTitle;
        }

        private string callToActionDescription(bool? impressionIsPositive)
        {
            if (impressionIsPositive == null)
                return string.Empty;

            return impressionIsPositive.Value
                   ? Resources.RatingViewPositiveCallToActionDescription
                   : Resources.RatingViewNegativeCallToActionDescription;
        }

        private string callToActionButtonTitle(bool? impressionIsPositive)
        {
            if (impressionIsPositive == null)
                return string.Empty;

            return impressionIsPositive.Value
                   ? Resources.RatingViewPositiveCallToActionButtonTitle
                   : Resources.RatingViewNegativeCallToActionButtonTitle;
        }

        private async Task performMainAction()
        {
            hide();

            if (!impressionWasRegistered)
                return;

            if (impressionIsPositive)
            {
                ratingService.AskForRating();
                /*
                 * We can't really know whether the user has actually rated.
                 * We only know that we presented the rating view (iOS)
                 * or navigated to the market (Android).
                */
                trackSecondStepOutcome(
                    RatingViewOutcome.AppWasRated,
                    RatingViewSecondStepOutcome.AppWasRated,
                    analyticsService.RatingViewSecondStepRate);
            }
            else
            {
                var sendFeedbackSucceed = await navigationService.Navigate<SendFeedbackViewModel, bool>();
                isFeedbackSuccessViewShowing.OnNext(sendFeedbackSucceed);

                trackSecondStepOutcome(
                    RatingViewOutcome.FeedbackWasLeft,
                    RatingViewSecondStepOutcome.FeedbackWasLeft,
                    analyticsService.RatingViewSecondStepSendFeedback);
            }
        }

        public void Dismiss()
        {
            hide();

            if (!impressionWasRegistered)
                return;

            if (impressionIsPositive)
            {
                trackSecondStepOutcome(
                    RatingViewOutcome.AppWasNotRated,
                    RatingViewSecondStepOutcome.AppWasNotRated,
                    analyticsService.RatingViewSecondStepDontRate);
            }
            else
            {
                trackSecondStepOutcome(
                    RatingViewOutcome.FeedbackWasNotLeft,
                    RatingViewSecondStepOutcome.FeedbackWasNotLeft,
                    analyticsService.RatingViewSecondStepDontSendFeedback);
            }
        }

        private void trackSecondStepOutcome(RatingViewOutcome outcome, RatingViewSecondStepOutcome genericEventParameter, IAnalyticsEvent specificEvent)
        {
            trackStepOutcome(outcome, specificEvent);
            analyticsService.UserFinishedRatingViewSecondStep.Track(genericEventParameter);
        }

        private void trackStepOutcome(RatingViewOutcome outcome, IAnalyticsEvent specificEvent)
        {
            onboardingStorage.SetRatingViewOutcome(outcome, timeService.CurrentDateTime);
            specificEvent.Track();
        }

        private void hide()
        {
            navigationService.ChangePresentation(
                ToggleRatingViewVisibilityHint.Hide()
            );
        }
    }
}
