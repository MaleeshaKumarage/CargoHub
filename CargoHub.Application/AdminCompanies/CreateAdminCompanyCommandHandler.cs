using CargoHub.Application.Company;
using MediatR;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Application.AdminCompanies;

public sealed class CreateAdminCompanyCommandHandler : IRequestHandler<CreateAdminCompanyCommand, AdminCompanyMutationResult>
{
    private readonly ICompanyRepository _companies;
    private readonly ICompanyAdminInviteIssuer _inviteIssuer;
    private readonly ICompanyUserMetrics _metrics;

    public CreateAdminCompanyCommandHandler(
        ICompanyRepository companies,
        ICompanyAdminInviteIssuer inviteIssuer,
        ICompanyUserMetrics metrics)
    {
        _companies = companies;
        _inviteIssuer = inviteIssuer;
        _metrics = metrics;
    }

    public async Task<AdminCompanyMutationResult> Handle(CreateAdminCompanyCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? "";
        var bid = request.BusinessId?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
            return Fail("NameRequired", "Company name is required.");
        if (string.IsNullOrEmpty(bid))
            return Fail("BusinessIdRequired", "Business ID is required for invites and registration.");

        if (request.MaxUserAccounts is < 1)
            return Fail("InvalidMaxUsers", "Max user accounts must be at least 1 when set.");
        if (request.MaxAdminAccounts is < 1)
            return Fail("InvalidMaxAdmins", "Max admin accounts must be at least 1 when set.");

        var dup = await _companies.GetByBusinessIdAsync(bid, cancellationToken);
        if (dup != null)
            return Fail("BusinessIdExists", "A company with this Business ID already exists.");

        var explicitEmail = string.IsNullOrWhiteSpace(request.InitialAdminEmail)
            ? null
            : request.InitialAdminEmail.Trim();

        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            BusinessId = bid,
            CompanyId = "", // handler fills after gen
            MaxUserAccounts = request.MaxUserAccounts,
            MaxAdminAccounts = request.MaxAdminAccounts,
            InitialAdminInviteEmail = explicitEmail
        };
        if (string.IsNullOrWhiteSpace(company.CompanyId))
            company.CompanyId = company.Id.ToString("N");

        company = await _companies.CreateAsync(company, cancellationToken);

        var admins = await _metrics.CountAdminsForBusinessIdAsync(bid, cancellationToken);
        if (admins == 0)
            await _inviteIssuer.TryIssueInitialAdminInviteAsync(company.Id, bid, explicitEmail, cancellationToken);

        return await ToSuccessAsync(company.Id, cancellationToken);
    }

    private async Task<AdminCompanyMutationResult> ToSuccessAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var c = await _companies.GetByIdAsync(companyId, cancellationToken);
        if (c == null)
            return Fail("NotFound", "Company was not found after create.");

        var bid = c.BusinessId ?? "";
        var users = string.IsNullOrEmpty(bid) ? 0 : await _metrics.CountActiveUsersForBusinessIdAsync(bid, cancellationToken);
        var admins = string.IsNullOrEmpty(bid) ? 0 : await _metrics.CountAdminsForBusinessIdAsync(bid, cancellationToken);

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
                AdminCount = admins
            }
        };
    }

    private static AdminCompanyMutationResult Fail(string code, string message) =>
        new() { Success = false, ErrorCode = code, Message = message };
}
