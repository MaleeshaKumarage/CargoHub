using CargoHub.Domain.Companies;

namespace CargoHub.Application.Company;

/// <summary>
/// Persistence for company admin invite tokens.
/// </summary>
public interface ICompanyAdminInviteRepository
{
    Task AddAsync(CompanyAdminInvite invite, CancellationToken cancellationToken = default);

    /// <summary>Valid, non-expired, not consumed invite matching token hash.</summary>
    Task<CompanyAdminInvite?> GetActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task MarkConsumedAsync(Guid inviteId, CancellationToken cancellationToken = default);

    /// <summary>Invalidate pending invites for this company (e.g. before issuing a new one).</summary>
    Task RevokePendingForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
