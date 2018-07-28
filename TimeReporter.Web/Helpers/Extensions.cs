using System;

namespace TimeReporter.Web.Extensions
{
    public static class Extensions
    {
        public static string ToTimeZoneString(this DateTime utcDate, TimeZoneInfo timeZone, string format = "HH:mm")
        {
            if (timeZone == null)
            {
                return utcDate.ToLocalTime().ToString(format);
            }
            {
                return TimeZoneInfo.ConvertTimeFromUtc(utcDate, timeZone).ToString(format);
            }
        }
    }
}