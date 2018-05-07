using System;
using System.Globalization;

namespace GitHub.Unity
{
    static class DateTimeExtensions
    {
        public static DateTimeOffset ToDateTimeOffset(this string dateString, DateTimeOffset? @default = null)
        {
            DateTimeOffset result;
            if (DateTimeOffset.TryParseExact(dateString, Constants.Iso8601Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return result;
            }
            return @default.HasValue ? @default.Value : DateTimeOffset.MinValue;
        }

        public static string ToIsoString(this DateTimeOffset dt)
        {
            return dt.ToString(Constants.Iso8601Format);
        }
    }
}
