namespace CargoHub.Domain.Bookings;

/// <summary>
/// Tracks when each booking milestone status was reached. Separate table for audit and timeline.
/// </summary>
public class BookingStatusHistory
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    /// <summary>Milestone: Draft, CompletedBooking, Waybill, SendBooking, Confirmed, Delivered.</summary>
    public string Status { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    /// <summary>Optional source: draft_created, draft_confirmed, waybill_printed, etc.</summary>
    public string? Source { get; set; }
}

/// <summary>Well-known status values for booking milestones.</summary>
public static class BookingStatus
{
    public const string Draft = "Draft";
    public const string CompletedBooking = "CompletedBooking";
    public const string Waybill = "Waybill";
    public const string SendBooking = "SendBooking";
    public const string Confirmed = "Confirmed";
    public const string Delivered = "Delivered";

    public static readonly string[] All = { Draft, CompletedBooking, Waybill, SendBooking, Confirmed, Delivered };
}
