using TimeZoneConverter;

namespace CargoHub.Api.Services;

public static class DailyDigestTimeHelper
{
    /// <summary>UTC range [from, to) for one calendar day in <paramref name="timeZoneId"/>.</summary>
    public static (DateTime FromUtc, DateTime ToUtc) GetUtcRangeForLocalDate(DateOnly localDate, string timeZoneId)
    {
        var tz = TZConvert.GetTimeZoneInfo(timeZoneId);
        var startLocal = DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var endLocal = DateTime.SpecifyKind(localDate.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, tz);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, tz);
        return (fromUtc, toUtc);
    }

    /// <summary>Calendar date in <paramref name="timeZoneId"/> for "now" (typically derive yesterday from this at run time).</summary>
    public static DateOnly GetLocalDateToday(string timeZoneId, DateTime utcNow)
    {
        var tz = TZConvert.GetTimeZoneInfo(timeZoneId);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        return DateOnly.FromDateTime(local.Date);
    }

    public static bool TryParseRunAtLocalTime(string? s, out TimeSpan timeOfDay)
    {
        timeOfDay = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var parts = s.Trim().Split(':');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out var h) || !int.TryParse(parts[1], out var m)) return false;
        if (h is < 0 or > 23 || m is < 0 or > 59) return false;
        timeOfDay = new TimeSpan(h, m, 0);
        return true;
    }

    /// <summary>Next UTC instant when local time in <paramref name="timeZoneId"/> equals <paramref name="runAtLocalTime"/>.</summary>
    public static DateTime GetNextRunUtc(string timeZoneId, TimeSpan runAtLocalTime, DateTime utcNow)
    {
        var tz = TZConvert.GetTimeZoneInfo(timeZoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        var todayRun = localNow.Date + runAtLocalTime;
        var nextLocal = localNow < todayRun ? todayRun : todayRun.AddDays(1);
        var unspecified = DateTime.SpecifyKind(nextLocal, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, tz);
    }
}
