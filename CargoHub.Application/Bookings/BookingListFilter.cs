namespace CargoHub.Application.Bookings;

/// <summary>
/// Optional filters for listing bookings.
/// </summary>
/// <param name="Search">Search in ShipmentNumber, WaybillNumber, CustomerName (contains, case-insensitive).</param>
/// <param name="CreatedFrom">Include only bookings created on or after this date (UTC).</param>
/// <param name="CreatedTo">Include only bookings created on or before this date (UTC).</param>
/// <param name="Enabled">Filter by enabled status. Null = all, true = active only, false = disabled only. Applies to completed bookings only.</param>
public sealed record BookingListFilter(
    string? Search = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null,
    bool? Enabled = null
);
