namespace HiavaNet.Application.Couriers;

/// <summary>
/// Resolves the correct courier client by courier id (e.g. Header.PostalService).
/// </summary>
public interface ICourierBookingClientFactory
{
    /// <summary>
    /// Returns the client for the given courier, or null if not registered.
    /// </summary>
    ICourierBookingClient? GetClient(string courierId);

    /// <summary>
    /// All registered courier ids.
    /// </summary>
    IReadOnlyList<string> RegisteredCourierIds { get; }
}
