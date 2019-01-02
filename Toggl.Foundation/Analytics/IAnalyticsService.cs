using System;
using System.Collections.Generic;
using Toggl.Foundation.Sync;

namespace Toggl.Foundation.Analytics
{
    public interface IAnalyticsService
    {
        IAnalyticsEvent<AuthenticationMethod> Login { get; }

        IAnalyticsEvent<LoginErrorSource> LoginError { get; }

        IAnalyticsEvent<AuthenticationMethod> SignUp { get; }

        IAnalyticsEvent<SignUpErrorSource> SignUpError { get; }

        IAnalyticsEvent<LoginSignupAuthenticationMethod> UserIsMissingApiToken { get; }

        IAnalyticsEvent<string> OnboardingSkip { get; }

        IAnalyticsEvent<LogoutSource> Logout { get; }

        IAnalyticsEvent ResetPassword { get; }

        IAnalyticsEvent PasswordManagerButtonClicked { get; }

        IAnalyticsEvent PasswordManagerContainsValidEmail { get; }

        IAnalyticsEvent PasswordManagerContainsValidPassword { get; }

        IAnalyticsEvent<Type> CurrentPage { get; }

        IAnalyticsEvent<TimeEntryStartOrigin> TimeEntryStarted { get; }

        IAnalyticsEvent<TimeEntryStopOrigin> TimeEntryStopped { get; }

        IAnalyticsEvent RatingViewWasShown { get; }

        IAnalyticsEvent<bool> UserFinishedRatingViewFirstStep { get; }

        IAnalyticsEvent<RatingViewSecondStepOutcome> UserFinishedRatingViewSecondStep { get; }

        IAnalyticsEvent RatingViewFirstStepLike { get; }

        IAnalyticsEvent RatingViewFirstStepDislike { get; }

        IAnalyticsEvent RatingViewSecondStepRate { get; }

        IAnalyticsEvent RatingViewSecondStepDontRate { get; }

        IAnalyticsEvent RatingViewSecondStepSendFeedback { get; }

        IAnalyticsEvent RatingViewSecondStepDontSendFeedback { get; }

        IAnalyticsEvent DeleteTimeEntry { get; }

        IAnalyticsEvent<string> ApplicationShortcut { get; }

        IAnalyticsEvent EditEntrySelectProject { get; }

        IAnalyticsEvent EditEntrySelectTag { get; }

        IAnalyticsEvent<ProjectTagSuggestionSource> StartEntrySelectProject { get; }

        IAnalyticsEvent<ProjectTagSuggestionSource> StartEntrySelectTag { get; }

        IAnalyticsEvent<ReportsSource, int, int, double> ReportsSuccess { get; }

        IAnalyticsEvent<ReportsSource, int, double> ReportsFailure { get; }

        IAnalyticsEvent OfflineModeDetected { get; }

        IAnalyticsEvent<int> ProjectPlaceholdersCreated { get; }

        IAnalyticsEvent<EditViewTapSource> EditViewTapped { get; }

        IAnalyticsEvent<StartViewTapSource> StartViewTapped { get; }

        IAnalyticsEvent LostWorkspaceAccess { get; }

        IAnalyticsEvent GainWorkspaceAccess { get; }

        IAnalyticsEvent<string> WorkspaceSyncError { get; }

        IAnalyticsEvent<string> UserSyncError { get; }

        IAnalyticsEvent<string> WorkspaceFeaturesSyncError { get; }

        IAnalyticsEvent<string> PreferencesSyncError { get; }

        IAnalyticsEvent<string> TagsSyncError { get; }

        IAnalyticsEvent<string> ClientsSyncError { get; }

        IAnalyticsEvent<string> ProjectsSyncError { get; }

        IAnalyticsEvent<string> TasksSyncError { get; }

        IAnalyticsEvent<string> TimeEntrySyncError { get; }

        IAnalyticsEvent<PushSyncOperation, string> EntitySynced { get; }

        IAnalyticsEvent<string, string> EntitySyncStatus { get; }

        IAnalyticsEvent NoDefaultWorkspace { get; }

        IAnalyticsEvent<string, string> HandledException { get; }

        IAnalyticsEvent TwoRunningTimeEntriesInconsistencyFixed { get; }

        IAnalyticsEvent CalendarOnboardingStarted { get; }

        IAnalyticsEvent EditViewOpenedFromCalendar { get; }

        IAnalyticsEvent<CalendarChangeEvent> TimeEntryChangedFromCalendar { get; }

        IAnalyticsEvent<int> ProjectsInaccesibleAfterCleanUp { get; }

        IAnalyticsEvent<int> TagsInaccesibleAfterCleanUp { get; }

        IAnalyticsEvent<int> TasksInaccesibleAfterCleanUp { get; }

        IAnalyticsEvent<int> ClientsInaccesibleAfterCleanUp { get; }

        IAnalyticsEvent<int> TimeEntriesInaccesibleAfterCleanUp { get; }

        IAnalyticsEvent<int> WorkspacesInaccesibleAfterCleanUp { get; }

        IAnalyticsEvent<int> NumberOfSyncedTimeEntriesWhenResumingTheAppFromBackground { get; }

        IAnalyticsEvent BackgroundSyncStarted { get; }

        IAnalyticsEvent<string> BackgroundSyncFinished { get; }

        IAnalyticsEvent<string, string, string> BackgroundSyncFailed { get; }

        IAnalyticsEvent<int> RateLimitingDelayDuringSyncing { get; }

        IAnalyticsEvent<string, string> UnknownLoginFailure { get; }

        IAnalyticsEvent<string, string> UnknownSignUpFailure { get; }

        void Track(string eventName, Dictionary<string, string> parameters = null);

        void TrackAnonymized(Exception exception);

        void Track(Exception exception, string message);

        void Track(ITrackableEvent trackableEvent);
    }
}
