namespace CargoHub.Application.AdminCompanies;

/// <summary>
/// Demotes or deactivates company-scoped users when Super Admin lowers account caps.
/// </summary>
public interface IAdminCompanyLimitUserOperations
{
    /// <summary>
    /// Demote each user from Admin to User (must belong to <paramref name="businessId"/>, active, not SuperAdmin).
    /// </summary>
    Task<string?> DemoteAdminsAsync(string businessId, IReadOnlyList<string> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate each user account (same scope rules).
    /// </summary>
    Task<string?> DeactivateUsersAsync(string businessId, IReadOnlyList<string> userIds, CancellationToken cancellationToken = default);
}
