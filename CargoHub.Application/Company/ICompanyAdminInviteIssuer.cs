namespace CargoHub.Application.Company;

/// <summary>
/// Issues the initial company admin invite when there are no admins yet.
/// </summary>
public interface ICompanyAdminInviteIssuer
{
    /// <param name="explicitSuperAdminEmail">When set, invite this address; otherwise use configured fallback <c>{businessId}@domain</c>.</param>
    Task TryIssueInitialAdminInviteAsync(
        Guid companyId,
        string businessId,
        string? explicitSuperAdminEmail,
        CancellationToken cancellationToken = default);
}
