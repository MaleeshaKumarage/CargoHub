namespace CargoHub.Application.Bookings;

/// <summary>
/// Sends the daily per-company digest for a single local calendar date (typically "yesterday" in the configured zone).
/// </summary>
public interface IDailyBookingDigestOrchestrator
{
    Task ProcessDigestForLocalDateAsync(DateOnly digestLocalDate, string timeZoneId, bool skipIfEmpty, CancellationToken cancellationToken = default);
}
