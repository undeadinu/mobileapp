using System;
using System.Collections.Generic;
using System.Reactive;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Suggestions;
using Toggl.Multivac;

namespace Toggl.Foundation.Interactors
{
    public interface IInteractorFactory
    {
        #region Time Entries

        IInteractor<IObservable<IThreadSafeTimeEntry>> CreateTimeEntry(ITimeEntryPrototype prototype);

        IInteractor<IObservable<IThreadSafeTimeEntry>> StartSuggestion(Suggestion suggestion);

        IInteractor<IObservable<IThreadSafeTimeEntry>> ContinueTimeEntry(ITimeEntryPrototype prototype);

        IInteractor<IObservable<IThreadSafeTimeEntry>> ContinueMostRecentTimeEntry();

        IInteractor<IObservable<IThreadSafeTimeEntry>> UpdateTimeEntry(EditTimeEntryDto dto);

        IInteractor<IObservable<Unit>> DeleteTimeEntry(long id);

        IInteractor<IObservable<IEnumerable<IThreadSafeTimeEntry>>> GetAllTimeEntriesVisibleToTheUser();

        IInteractor<IObservable<IThreadSafeTimeEntry>> StopTimeEntry(DateTimeOffset currentDateTime, TimeEntryStopOrigin origin);

        IInteractor<IObservable<Unit>> ObserveTimeEntriesChanges();

        #endregion

        #region Projects

        IInteractor<IObservable<bool>> ProjectDefaultsToBillable(long projectId);

        IInteractor<IObservable<bool>> IsBillableAvailableForProject(long projectId);

        IInteractor<IObservable<IThreadSafeProject>> CreateProject(CreateProjectDTO dto);

        #endregion

        #region Workspaces

        IInteractor<IObservable<IThreadSafeWorkspace>> GetDefaultWorkspace();

        IInteractor<IObservable<Unit>> SetDefaultWorkspace(long workspaceId);

        IInteractor<IObservable<IEnumerable<IThreadSafeWorkspace>>> GetAllWorkspaces();

        IInteractor<IObservable<IThreadSafeWorkspace>> GetWorkspaceById(long workspaceId);

        IInteractor<IObservable<bool?>> AreProjectsBillableByDefault(long workspaceId);

        IInteractor<IObservable<bool>> WorkspaceAllowsBillableRates(long workspaceId);

        IInteractor<IObservable<bool>> AreCustomColorsEnabledForWorkspace(long workspaceId);

        IInteractor<IObservable<bool>> IsBillableAvailableForWorkspace(long workspaceId);

        IInteractor<IObservable<Unit>> CreateDefaultWorkspace();

        IInteractor<IObservable<IEnumerable<IThreadSafeWorkspace>>> ObserveAllWorkspaces();

        IInteractor<IObservable<Unit>> ObserveWorkspacesChanges();

        #endregion

        #region Sync

        IInteractor<IObservable<IEnumerable<SyncFailureItem>>> GetItemsThatFailedToSync();

        IInteractor<IObservable<bool>> HasFinishedSyncBefore();

        IInteractor<IObservable<SyncOutcome>> RunBackgroundSync();

        #endregion

        #region Autocomplete Suggestions

        IInteractor<IObservable<IEnumerable<AutocompleteSuggestion>>> GetTimeEntriesAutocompleteSuggestions(
            IList<string> wordsToQuery);

        IInteractor<IObservable<IEnumerable<AutocompleteSuggestion>>> GetTagsAutocompleteSuggestions(
            IList<string> wordsToQuery);

        IInteractor<IObservable<IEnumerable<AutocompleteSuggestion>>> GetProjectsAutocompleteSuggestions(
            IList<string> wordsToQuery);

        #endregion

        #region Preferences

        IInteractor<IObservable<IThreadSafePreferences>> GetPreferences();

        IInteractor<IObservable<IThreadSafePreferences>> UpdatePreferences(EditPreferencesDTO dto);

        #endregion

        #region User

        IInteractor<IObservable<byte[]>> GetUserAvatar(string url);

        IInteractor<IObservable<IThreadSafeUser>> UpdateUser(EditUserDTO dto);

        IInteractor<IObservable<IThreadSafeUser>> UpdateDefaultWorkspace(long selectedWorkspaceId);

        #endregion

        #region Settings

        IInteractor<IObservable<Unit>> SendFeedback(string message);

        #endregion

        #region Calendar

        IInteractor<IObservable<CalendarItem>> GetCalendarItemWithId(string eventId);

        IInteractor<IObservable<IEnumerable<CalendarItem>>> GetCalendarItemsForDate(DateTime date);

        IInteractor<IObservable<IEnumerable<UserCalendar>>> GetUserCalendars();

        IInteractor<Unit> SetEnabledCalendars(params string[] ids);

        #endregion

        #region Notifications

        IInteractor<IObservable<Unit>> UnscheduleAllNotifications();

        IInteractor<IObservable<Unit>> ScheduleEventNotificationsForNextWeek();

        #endregion

        #region Clients

        IInteractor<IObservable<IThreadSafeClient>> CreateClient(string clientName, long workspaceId);

        IInteractor<IObservable<IEnumerable<IThreadSafeClient>>> GetAllClientsInWorkspace(long workspaceId);

        #endregion

        #region Tags

        IInteractor<IObservable<IThreadSafeTag>> CreateTag(string tagName, long workspaceId);

        #endregion

        #region Changes

        IInteractor<IObservable<Unit>> ObserveWorkspaceOrTimeEntriesChanges();

        #endregion
    }
}
