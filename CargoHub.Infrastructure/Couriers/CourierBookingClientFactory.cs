using CargoHub.Application.Couriers;

namespace CargoHub.Infrastructure.Couriers;

/// <summary>
/// Resolves courier clients by id from the set of registered clients.
/// </summary>
public sealed class CourierBookingClientFactory : ICourierBookingClientFactory
{
    private readonly IReadOnlyDictionary<string, ICourierBookingClient> _clients;

    public CourierBookingClientFactory(IEnumerable<ICourierBookingClient> clients)
    {
        _clients = clients.ToDictionary(c => c.CourierId, StringComparer.OrdinalIgnoreCase);
    }

    public ICourierBookingClient? GetClient(string courierId) =>
        _clients.TryGetValue(courierId, out var client) ? client : null;

    public IReadOnlyList<string> RegisteredCourierIds => _clients.Keys.ToList();
}
