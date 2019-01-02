using System;
using System.Collections.Generic;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Sync;
using Toggl.Multivac;

namespace Toggl.Foundation.Analytics
{
    [Preserve(AllMembers = true)]
    public abstract class BaseAnalyticsService : AnalyticsEventAttributeInitializer, IAnalyticsService
    {
        protected BaseAnalyticsService()
        {
            InitializeAttributedProperties(this);
        }

        [AnalyticsEvent("AuthenticationMethod")]
        public IAnalyticsEvent<AuthenticationMethod> Login { get; protected set; }

        [AnalyticsEvent("Source")]
        public IAnalyticsEvent<LoginErrorSource> LoginError { get; protected set; }

        [AnalyticsEvent("AuthenticationMethod")]
        public IAnalyticsEvent<AuthenticationMethod> SignUp { get; protected set; }

        [AnalyticsEvent("Source")]
        public IAnalyticsEvent<SignUpErrorSource> SignUpError { get; protected set; }

        [AnalyticsEvent("AuthenticationMethod")]
        public IAnalyticsEvent<LoginSignupAuthenticationMethod> UserIsMissingApiToken { get; protected set; }

        [AnalyticsEvent("PageWhenSkipWasClicked")]
        public IAnalyticsEvent<string> OnboardingSkip { get; protected set; }

        [AnalyticsEvent("Source")]
        public IAnalyticsEvent<LogoutSource> Logout { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent ResetPassword { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent PasswordManagerButtonClicked { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent PasswordManagerContainsValidEmail { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent PasswordManagerContainsValidPassword { get; protected set; }

        [AnalyticsEvent("CurrentPage")]
        public IAnalyticsEvent<Type> CurrentPage { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent DeleteTimeEntry { get; protected set; }

        [AnalyticsEvent("ApplicationShortcutType")]
        public IAnalyticsEvent<string> ApplicationShortcut { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent EditEntrySelectProject { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent EditEntrySelectTag { get; protected set; }

        [AnalyticsEvent("Source")]
        public IAnalyticsEvent<ProjectTagSuggestionSource> StartEntrySelectProject { get; protected set; }

        [AnalyticsEvent("Source")]
        public IAnalyticsEvent<ProjectTagSuggestionSource> StartEntrySelectTag { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent RatingViewWasShown { get; protected set; }

        [AnalyticsEvent("isPositive")]
        public IAnalyticsEvent<bool> UserFinishedRatingViewFirstStep { get; protected set; }

        [AnalyticsEvent("outcome")]
        public IAnalyticsEvent<RatingViewSecondStepOutcome> UserFinishedRatingViewSecondStep { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent RatingViewFirstStepLike { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent RatingViewFirstStepDislike { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent RatingViewSecondStepRate { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent RatingViewSecondStepDontRate { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent RatingViewSecondStepSendFeedback { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent RatingViewSecondStepDontSendFeedback { get; protected set; }

        [AnalyticsEvent("Source", "TotalDays", "ProjectsNotSynced", "LoadingTime")]
        public IAnalyticsEvent<ReportsSource, int, int, double> ReportsSuccess { get; protected set; }

        [AnalyticsEvent("Source", "TotalDays", "LoadingTime")]
        public IAnalyticsEvent<ReportsSource, int, double> ReportsFailure { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent OfflineModeDetected { get; protected set; }

        [AnalyticsEvent("TapSource")]
        public IAnalyticsEvent<EditViewTapSource> EditViewTapped { get; set; }

        [AnalyticsEvent("NumberOfCreatedPlaceholders")]
        public IAnalyticsEvent<int> ProjectPlaceholdersCreated { get; protected set; }

        [AnalyticsEvent("ExceptionType", "ExceptionMessage")]
        public IAnalyticsEvent<string, string> HandledException { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent TwoRunningTimeEntriesInconsistencyFixed { get; protected set; }

        [AnalyticsEvent("TapSource")]
        public IAnalyticsEvent<StartViewTapSource> StartViewTapped { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent NoDefaultWorkspace { get; protected set; }

        [AnalyticsEvent("Origin")]
        public IAnalyticsEvent<TimeEntryStartOrigin> TimeEntryStarted { get; protected set; }

        [AnalyticsEvent("Origin")]
        public IAnalyticsEvent<TimeEntryStopOrigin> TimeEntryStopped { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent LostWorkspaceAccess { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent GainWorkspaceAccess { get; protected set; }

        [AnalyticsEvent("Reason")]
        public IAnalyticsEvent<string> WorkspaceSyncError { get; protected set; }

        [AnalyticsEvent("Reason")]
        public IAnalyticsEvent<string> UserSyncError { get; protected set; }

        [AnalyticsEvent("Reason")]
        public IAnalyticsEvent<string> WorkspaceFeaturesSyncError { get; protected set; }

        [AnalyticsEvent("Reason")]
        public IAnalyticsEvent<string> PreferencesSyncError { get; protected set; }

        [AnalyticsEvent("Reason")]
        public IAnalyticsEvent<string> TagsSyncError { get; protected set; }

        [AnalyticsEvent("Reason")]
        public IAnalyticsEvent<string> ClientsSyncError { get; protected set; }

        [AnalyticsEvent("Reason")]
        public IAnalyticsEvent<string> ProjectsSyncError { get; protected set; }

        [AnalyticsEvent("Reason")]
        public IAnalyticsEvent<string> TasksSyncError { get; protected set; }

        [AnalyticsEvent("Reason")]
        public IAnalyticsEvent<string> TimeEntrySyncError { get; protected set; }

        [AnalyticsEvent("Method", "Entity")]
        public IAnalyticsEvent<PushSyncOperation, string> EntitySynced { get; protected set; }

        [AnalyticsEvent("Entity", "Status")]
        public IAnalyticsEvent<string, string> EntitySyncStatus { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent CalendarOnboardingStarted { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent EditViewOpenedFromCalendar { get; protected set; }

        [AnalyticsEvent("ChangeEvent")]
        public IAnalyticsEvent<CalendarChangeEvent> TimeEntryChangedFromCalendar { get; protected set; }

        [AnalyticsEvent("NumberOfProjectsInaccesibleAfterCleanUp")]
        public IAnalyticsEvent<int> ProjectsInaccesibleAfterCleanUp { get; protected set; }

        [AnalyticsEvent("NumberOfTagsInaccesibleAfterCleanUp")]
        public IAnalyticsEvent<int> TagsInaccesibleAfterCleanUp { get; protected set; }

        [AnalyticsEvent("NumberOfTasksInaccesibleAfterCleanUp")]
        public IAnalyticsEvent<int> TasksInaccesibleAfterCleanUp { get; protected set; }

        [AnalyticsEvent("NumberOfClientsInaccesibleAfterCleanUp")]
        public IAnalyticsEvent<int> ClientsInaccesibleAfterCleanUp { get; protected set; }

        [AnalyticsEvent("NumberOfTimeEntriesInaccesibleAfterCleanUp")]
        public IAnalyticsEvent<int> TimeEntriesInaccesibleAfterCleanUp { get; protected set; }

        [AnalyticsEvent("NumberOfWorkspacesInaccesibleAfterCleanUp")]
        public IAnalyticsEvent<int> WorkspacesInaccesibleAfterCleanUp { get; protected set; }

        [AnalyticsEvent("SyncedTimeEntries")]
        public IAnalyticsEvent<int> NumberOfSyncedTimeEntriesWhenResumingTheAppFromBackground { get; protected set; }

        [AnalyticsEvent]
        public IAnalyticsEvent BackgroundSyncStarted { get; protected set; }

        [AnalyticsEvent("BackgroundSyncFinishedWithOutcome")]
        public IAnalyticsEvent<string> BackgroundSyncFinished { get; protected set; }

        [AnalyticsEvent("Type", "Message", "StackTrace")]
        public IAnalyticsEvent<string, string, string> BackgroundSyncFailed { get; protected set; }

        [AnalyticsEvent("Type", "Message")]
        public IAnalyticsEvent<string, string> UnknownLoginFailure { get; protected set; }

        [AnalyticsEvent("Type", "Message")]
        public IAnalyticsEvent<string, string> UnknownSignUpFailure { get; protected set; }

        [AnalyticsEvent("DelayDurationSeconds")]
        public IAnalyticsEvent<int> RateLimitingDelayDuringSyncing { get; protected set; }

        public void TrackAnonymized(Exception exception)
        {
            if (exception.IsAnonymized())
            {
                TrackException(exception);
            }
            else
            {
                HandledException.Track(exception.GetType().FullName, exception.Message);
            }
        }

        public abstract void Track(Exception exception, string message);

        public abstract void Track(string eventName, Dictionary<string, string> parameters = null);

        public void Track(ITrackableEvent trackableEvent)
            => Track(trackableEvent.EventName, trackableEvent.ToDictionary());

        protected abstract void TrackException(Exception exception);
    }
}
