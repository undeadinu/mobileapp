using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Models;

namespace Toggl.Foundation.Tests.Sync.Extensions
{
    public static class IPreferencesExtensions
    {
        public static IPreferences With(this IPreferences preferences,
            New<DurationFormat> durationFormat = default(New<DurationFormat>))
            => new Preferences
            {
                TimeOfDayFormat = preferences.TimeOfDayFormat,
                DateFormat = preferences.DateFormat,
                DurationFormat = durationFormat.ValueOr(preferences.DurationFormat),
                CollapseTimeEntries = preferences.CollapseTimeEntries
            };

        public static IThreadSafePreferences ToSyncable(this IPreferences preferences)
            => new MockPreferences
            {
                CollapseTimeEntries = preferences.CollapseTimeEntries,
                DateFormat = preferences.DateFormat,
                DurationFormat = preferences.DurationFormat,
                Id = 0,
                IsDeleted = false,
                LastSyncErrorMessage = null,
                SyncStatus = SyncStatus.InSync,
                TimeOfDayFormat = preferences.TimeOfDayFormat
            };
    }
}
