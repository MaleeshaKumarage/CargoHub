using CargoHub.Application.Billing;
using CargoHub.Application.Billing.Admin;
using CargoHub.Application.Company;
using MediatR;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Application.AdminCompanies;

public sealed class CreateAdminCompanyCommandHandler : IRequestHandler<CreateAdminCompanyCommand, AdminCompanyMutationResult>
{
    private readonly ICompanyRepository _companies;
    private readonly ICompanyAdminInviteIssuer _inviteIssuer;
    private readonly ICompanyAdminInviteRepository _invites;
    private readonly ICompanyUserMetrics _metrics;
    private readonly ICompanySubscriptionAssignmentRepository _subscriptionAssignments;

    public CreateAdminCompanyCommandHandler(
        ICompanyRepository companies,
        ICompanyAdminInviteIssuer inviteIssuer,
        ICompanyAdminInviteRepository invites,
        ICompanyUserMetrics metrics,
        ICompanySubscriptionAssignmentRepository subscriptionAssignments)
    {
        _companies = companies;
        _inviteIssuer = inviteIssuer;
        _invites = invites;
        _metrics = metrics;
        _subscriptionAssignments = subscriptionAssignments;
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

        var inviteEmails = CompanyAdminInviteEmailsHelper.NormalizeList(request.InitialAdminEmails);
        if (request.MaxAdminAccounts is int maxAdmins && inviteEmails.Count > maxAdmins)
            return Fail("TooManyInviteEmails", "Number of admin emails cannot exceed max admin accounts.");

        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            BusinessId = bid,
            CompanyId = "", // handler fills after gen
            MaxUserAccounts = request.MaxUserAccounts,
            MaxAdminAccounts = request.MaxAdminAccounts,
            SubscriptionPlanId = request.SubscriptionPlanId ?? SubscriptionBillingConstants.DefaultTrialPlanId,
            InitialAdminInviteEmail = inviteEmails.FirstOrDefault(),
            InitialAdminInviteEmailsJson = CompanyAdminInviteEmailsHelper.SerializeJson(inviteEmails)
        };
        if (string.IsNullOrWhiteSpace(company.CompanyId))
            company.CompanyId = company.Id.ToString("N");

        company = await _companies.CreateAsync(company, cancellationToken);

        if (company.SubscriptionPlanId is { } initialPlanId)
        {
            await _subscriptionAssignments.RecordAsync(
                company.Id,
                initialPlanId,
                DateTime.UtcNow,
                null,
                cancellationToken);
        }

        var admins = await _metrics.CountAdminsForBusinessIdAsync(bid, cancellationToken);
        if (admins == 0)
            await _inviteIssuer.TryIssueInitialAdminInvitesAsync(company.Id, bid, inviteEmails.Count > 0 ? inviteEmails : null, cancellationToken);

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

        var inviteList = CompanyAdminInviteEmailsHelper.GetExplicitTargets(c.InitialAdminInviteEmailsJson, c.InitialAdminInviteEmail);
        var pendingInvites = await _invites.CountPendingValidInvitesAsync(c.Id, cancellationToken);
        var lastInviteAt = await _invites.GetLastInviteCreatedAtAsync(c.Id, cancellationToken);

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
                InitialAdminInviteEmails = inviteList.Count > 0 ? inviteList.ToList() : null,
                ActiveUserCount = users,
                AdminCount = admins,
                SubscriptionPlanId = c.SubscriptionPlanId,
                IsActive = c.IsActive,
                AdminInviteFirstAcceptedAtUtc = c.AdminInviteFirstAcceptedAtUtc,
                PendingAdminInviteCount = pendingInvites,
                LastAdminInviteCreatedAtUtc = lastInviteAt
            }
        };
    }

    private static AdminCompanyMutationResult Fail(string code, string message) =>
        new() { Success = false, ErrorCode = code, Message = message };
}
