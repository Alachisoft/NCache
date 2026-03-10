using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Licensing
{
    public class CloudDateTimeHelper
    {
        private static string DATE_TIME_FORMAT = "yyyy-MM-ddTHH";
        private static string USER_DATE_TIME_FORMAT = "MMMM dd, yyyy";

        public static string GetDateTimeUtcNowInString()
        {
            return ConvertDateTimeToString(DateTime.UtcNow);
        }
        public static string ConvertDateTimeToString(DateTime dateTime)
        {
            return dateTime.ToString(DATE_TIME_FORMAT);
        }
        public static DateTime ConvertToUtcDateTimeFormat(string dateTime)
        {
            DateTime date = DateTime.ParseExact(dateTime, DATE_TIME_FORMAT, null, System.Globalization.DateTimeStyles.None);
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }

        public static string ConvertToUserFriendlyDateTimeFormat(string dateTime)
        {
            return ConvertToUtcDateTimeFormat(dateTime).ToLocalTime().ToString(USER_DATE_TIME_FORMAT);
        }

        public static DateTime? ConvertToUtcDateTimeFormatSafe(string dateTime)
        {
            DateTime graceTime;
            if (DateTime.TryParseExact(dateTime, DATE_TIME_FORMAT, null, System.Globalization.DateTimeStyles.None, out graceTime))
            {
                return graceTime;
            }
            return null;
        }
    }
}
