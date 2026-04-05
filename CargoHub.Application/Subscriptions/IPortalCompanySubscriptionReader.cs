namespace CargoHub.Application.Subscriptions;

public interface IPortalCompanySubscriptionReader
{
    /// <summary>Loads plan and current pricing period for the company with this business ID, or null if no company.</summary>
    Task<PortalCompanySubscriptionDto?> GetForBusinessIdAsync(string businessId, CancellationToken cancellationToken = default);
}
