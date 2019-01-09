using System;
using System.Collections.Generic;

namespace Toggl.PrimeRadiant.Settings
{
    public interface IUserPreferences
    {
        IObservable<bool> IsManualModeEnabledObservable { get; }

        IObservable<List<string>> EnabledCalendars { get; }

        IObservable<bool> CalendarNotificationsEnabled { get; }

        IObservable<TimeSpan> TimeSpanBeforeCalendarNotifications { get; }

        IObservable<bool> AreRunningTimerNotificationsEnabledObservable { get; }

        IObservable<bool> AreStoppedTimerNotificationsEnabledObservable { get; }

        bool IsManualModeEnabled { get; }

        bool AreRunningTimerNotificationsEnabled { get; }

        bool AreStoppedTimerNotificationsEnabled { get; }

        void EnableManualMode();

        void EnableTimerMode();

        void SetRunningTimerNotifications(bool state);

        void SetStoppedTimerNotifications(bool state);

        void Reset();

        List<string> EnabledCalendarIds();

        void SetEnabledCalendars(params string[] ids);

        void SetCalendarNotificationsEnabled(bool enabled);

        void SetTimeSpanBeforeCalendarNotifications(TimeSpan timeSpan);
    }
}
