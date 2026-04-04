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

    /// <summary>
    /// When <paramref name="explicitEmails"/> is null or empty, sends one invite to the fallback <c>{businessId}@domain</c>.
    /// Otherwise sends one invite per distinct non-empty email (pending invites for the same company+email are replaced).
    /// </summary>
    Task TryIssueInitialAdminInvitesAsync(
        Guid companyId,
        string businessId,
        IReadOnlyList<string>? explicitEmails,
        CancellationToken cancellationToken = default);
}
