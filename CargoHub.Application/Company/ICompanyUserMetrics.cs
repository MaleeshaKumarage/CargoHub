namespace CargoHub.Application.Company;

/// <summary>
/// Counts portal users per company (by government BusinessId).
/// </summary>
public interface ICompanyUserMetrics
{
    Task<int> CountActiveUsersForBusinessIdAsync(string businessId, CancellationToken cancellationToken = default);

    Task<int> CountAdminsForBusinessIdAsync(string businessId, CancellationToken cancellationToken = default);
}
