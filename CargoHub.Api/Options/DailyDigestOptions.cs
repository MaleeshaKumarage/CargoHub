namespace CargoHub.Api.Options;

/// <summary>
/// Nightly digest: email company Admins a PDF of bookings created the previous local day.
/// </summary>
public class DailyDigestOptions
{
    public const string SectionName = "DailyDigest";

    /// <summary>When false, the background service does not run.</summary>
    public bool Enabled { get; set; }

    /// <summary>IANA id (e.g. UTC, Europe/Helsinki). On Windows, TimeZoneConverter resolves IANA to the OS zone.</summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>Local time of day to run after midnight, HH:mm (e.g. 00:05).</summary>
    public string RunAtLocalTime { get; set; } = "00:05";

    /// <summary>When true and there are no bookings for the day, skip email (claim row still recorded).</summary>
    public bool SkipIfEmpty { get; set; } = true;
}
