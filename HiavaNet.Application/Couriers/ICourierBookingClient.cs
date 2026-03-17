namespace HiavaNet.Application.Couriers;

/// <summary>
/// Common interface for all courier integrations (REST API, XML over HTTP, or email).
/// Each courier has one client implementation; orchestration uses this interface only.
/// </summary>
public interface ICourierBookingClient
{
    /// <summary>
    /// Unique courier identifier (e.g. "DHLExpress", "Matkahuolto", "HämeenTavarataxi").
    /// Must match the value used in booking header (e.g. Header.PostalService).
    /// </summary>
    string CourierId { get; }

    /// <summary>
    /// Submit a booking to the courier. Request is normalized; client maps to API/XML/email as needed.
    /// </summary>
    Task<CourierCreateResult> CreateBookingAsync(
        CourierCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get status/tracking for a shipment. Returns null if the courier does not support tracking (e.g. email-only).
    /// </summary>
    Task<CourierStatusResult?> GetStatusAsync(
        string carrierShipmentIdOrReference,
        CancellationToken cancellationToken = default);
}
