namespace CargoHub.Domain.Bookings;

/// <summary>
/// One row per company per local digest date per timezone — prevents duplicate digest sends after restarts.
/// </summary>
public class DailyDigestSendLog
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    /// <summary>Calendar date in the configured digest timezone (the "yesterday" window that was processed).</summary>
    public DateOnly DigestDateLocal { get; set; }
    /// <summary>Digest timezone id from configuration when this row was claimed.</summary>
    public string TimeZoneId { get; set; } = "";
    public DateTime SentAtUtc { get; set; }
}
