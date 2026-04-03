namespace CargoHub.Application.Bookings;

/// <summary>One row in the daily bookings digest PDF table.</summary>
public sealed class DailyDigestPdfRow
{
    public Guid BookingId { get; init; }
    public string Courier { get; init; } = "";
    public string ReceiverName { get; init; } = "";
    public string City { get; init; } = "";
    public string CreatedAtDisplay { get; init; } = "";
    public string CreatedBy { get; init; } = "";
    public string Status { get; init; } = "";
    public string? Reference { get; init; }
    public string? Waybill { get; init; }
    public bool IsDraft { get; init; }
}
