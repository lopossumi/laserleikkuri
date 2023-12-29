using System.Globalization;

public static class DateTimeExtensions
{
        public static string GetDateString(this DateTime dateTime) => dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static string GetIsoString(this DateTime dateTime) => dateTime.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
        public static string GetTimeString(this DateTime dateTime) => dateTime.ToString("HH:mm", CultureInfo.InvariantCulture);
}
