using System;

namespace Toggl.Foundation.MvvmCross.Transformations
{
    public class DateTimeToFormattedString
    {
        public static string Convert(DateTimeOffset date, string format, TimeZoneInfo timeZoneInfo = null)
        {
            if (timeZoneInfo == null)
            {
                timeZoneInfo = TimeZoneInfo.Local;
            }

            return getDateTimeOffsetInCorrectTimeZone(date, timeZoneInfo).ToString(format);
        }

        private static DateTimeOffset getDateTimeOffsetInCorrectTimeZone(DateTimeOffset value, TimeZoneInfo timeZone)
            => value == default(DateTimeOffset) ? value : TimeZoneInfo.ConvertTime(value, timeZone);
    }
}
