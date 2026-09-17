using System;

namespace Light.Extensions
{
    public static class DateTimeHelper
    {
        /// <summary>
        /// Convert DateTime to seconds Unix Timestamp
        /// </summary>
        public static long ToUnixTimeSeconds(this DateTime value)
        {
            var dateTimeOffset = new DateTimeOffset(value);
            return dateTimeOffset.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Convert DateTime to milliseconds Unix Timestamp
        /// </summary>
        public static long ToUnixTimeMilliseconds(this DateTime value)
        {
            var dateTimeOffset = new DateTimeOffset(value);
            return dateTimeOffset.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Convert seconds Unix Timestamp to DateTime 
        /// </summary>
        public static DateTimeOffset GetDateTimeFromSeconds(long value)
        {
            return DateTimeOffset.FromUnixTimeSeconds(value);
        }

        /// <summary>
        /// Convert milliseconds Unix Timestamp to DateTime 
        /// </summary>
        public static DateTimeOffset GetDateTimeFromMilliseconds(long value)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(value);
        }
    }
}
