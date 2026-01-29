namespace Fruitables.Helpers;

/// <summary>
/// Helper class for timezone conversion
/// </summary>
public static class DateTimeHelper
{
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    /// <summary>
    /// Convert UTC DateTime to Vietnam timezone (UTC+7)
    /// </summary>
    public static DateTime ToVietnamTime(this DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VietnamTimeZone);
    }

    /// <summary>
    /// Convert nullable UTC DateTime to Vietnam timezone
    /// </summary>
    public static DateTime? ToVietnamTime(this DateTime? utcDateTime)
    {
        return utcDateTime?.ToVietnamTime();
    }

    /// <summary>
    /// Format DateTime to Vietnam time string
    /// </summary>
    public static string ToVietnamTimeString(this DateTime utcDateTime, string format = "dd/MM/yyyy HH:mm")
    {
        return utcDateTime.ToVietnamTime().ToString(format);
    }

    /// <summary>
    /// Format nullable DateTime to Vietnam time string
    /// </summary>
    public static string ToVietnamTimeString(this DateTime? utcDateTime, string format = "dd/MM/yyyy HH:mm", string defaultValue = "—")
    {
        return utcDateTime.HasValue ? utcDateTime.Value.ToVietnamTimeString(format) : defaultValue;
    }
}
