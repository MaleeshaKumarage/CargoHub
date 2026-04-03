using CargoHub.Application.Company;
using MediatR;

namespace CargoHub.Application.AdminCompanies;

public sealed class UpdateAdminCompanyCommandHandler : IRequestHandler<UpdateAdminCompanyCommand, AdminCompanyMutationResult>
{
    private readonly ICompanyRepository _companies;
    private readonly ICompanyAdminInviteIssuer _inviteIssuer;
    private readonly ICompanyUserMetrics _metrics;

    public UpdateAdminCompanyCommandHandler(
        ICompanyRepository companies,
        ICompanyAdminInviteIssuer inviteIssuer,
        ICompanyUserMetrics metrics)
    {
        _companies = companies;
        _inviteIssuer = inviteIssuer;
        _metrics = metrics;
    }

    public async Task<AdminCompanyMutationResult> Handle(UpdateAdminCompanyCommand request, CancellationToken cancellationToken)
    {
        if (request.MaxUserAccounts is < 1)
            return Fail("InvalidMaxUsers", "Max user accounts must be at least 1 when set.");
        if (request.MaxAdminAccounts is < 1)
            return Fail("InvalidMaxAdmins", "Max admin accounts must be at least 1 when set.");

        var company = await _companies.GetByIdForUpdateAsync(request.CompanyId, cancellationToken);
        if (company == null)
            return Fail("NotFound", "Company not found.");

        if (request.MaxUserAccounts.HasValue)
            company.MaxUserAccounts = request.MaxUserAccounts;
        if (request.MaxAdminAccounts.HasValue)
            company.MaxAdminAccounts = request.MaxAdminAccounts;

        await _companies.UpdateAsync(company, cancellationToken);

        if (request.ResendAdminInvite)
        {
            var businessIdForInvite = company.BusinessId?.Trim();
            if (string.IsNullOrEmpty(businessIdForInvite))
                return Fail("BusinessIdRequired", "Company has no Business ID; cannot resend invite.");

            var admins = await _metrics.CountAdminsForBusinessIdAsync(businessIdForInvite, cancellationToken);
            if (admins == 0)
                await _inviteIssuer.TryIssueInitialAdminInviteAsync(company.Id, businessIdForInvite, company.InitialAdminInviteEmail, cancellationToken);
        }

        var c = await _companies.GetByIdAsync(request.CompanyId, cancellationToken);
        if (c == null)
            return Fail("NotFound", "Company not found after update.");

        var bid = c.BusinessId ?? "";
        var users = string.IsNullOrEmpty(bid) ? 0 : await _metrics.CountActiveUsersForBusinessIdAsync(bid, cancellationToken);
        var adminCount = string.IsNullOrEmpty(bid) ? 0 : await _metrics.CountAdminsForBusinessIdAsync(bid, cancellationToken);

        return new AdminCompanyMutationResult
        {
            Success = true,
            Company = new AdminCompanyDetailDto
            {
                Id = c.Id,
                Name = c.Name,
                BusinessId = c.BusinessId,
                CompanyId = c.CompanyId,
                MaxUserAccounts = c.MaxUserAccounts,
                MaxAdminAccounts = c.MaxAdminAccounts,
                InitialAdminInviteEmail = c.InitialAdminInviteEmail,
                ActiveUserCount = users,
                AdminCount = adminCount
            }
        };
    }

    private static AdminCompanyMutationResult Fail(string code, string message) =>
        new() { Success = false, ErrorCode = code, Message = message };
}
