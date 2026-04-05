using CargoHub.Application.Company;
using MediatR;

namespace CargoHub.Application.AdminCompanies;

public sealed class UpdateAdminCompanyCommandHandler : IRequestHandler<UpdateAdminCompanyCommand, AdminCompanyMutationResult>
{
    private readonly ICompanyRepository _companies;
    private readonly ICompanyAdminInviteIssuer _inviteIssuer;
    private readonly ICompanyUserMetrics _metrics;
    private readonly IAdminCompanyLimitUserOperations _limitUserOperations;

    public UpdateAdminCompanyCommandHandler(
        ICompanyRepository companies,
        ICompanyAdminInviteIssuer inviteIssuer,
        ICompanyUserMetrics metrics,
        IAdminCompanyLimitUserOperations limitUserOperations)
    {
        _companies = companies;
        _inviteIssuer = inviteIssuer;
        _metrics = metrics;
        _limitUserOperations = limitUserOperations;
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

        var bid = company.BusinessId?.Trim() ?? "";
        var currentActive = string.IsNullOrEmpty(bid) ? 0 : await _metrics.CountActiveUsersForBusinessIdAsync(bid, cancellationToken);
        var currentAdmins = string.IsNullOrEmpty(bid) ? 0 : await _metrics.CountAdminsForBusinessIdAsync(bid, cancellationToken);

        var effectiveMaxUsers = request.MaxUserAccounts ?? company.MaxUserAccounts;
        var effectiveMaxAdmins = request.MaxAdminAccounts ?? company.MaxAdminAccounts;

        var minDeactivate = effectiveMaxUsers is int mu && currentActive > mu ? currentActive - mu : 0;
        var minDemote = effectiveMaxAdmins is int ma && currentAdmins > ma ? currentAdmins - ma : 0;

        var demoteIds = NormalizeIds(request.DemoteAdminUserIds);
        var deactivateIds = NormalizeIds(request.DeactivateUserIds);

        if (minDemote > 0 && demoteIds.Count < minDemote)
            return LimitReductionConflict(bid, currentActive, effectiveMaxUsers, currentAdmins, effectiveMaxAdmins, minDeactivate, minDemote);

        if (minDeactivate > 0 && deactivateIds.Count < minDeactivate)
            return LimitReductionConflict(bid, currentActive, effectiveMaxUsers, currentAdmins, effectiveMaxAdmins, minDeactivate, minDemote);

        if (minDemote == 0 && demoteIds.Count > 0)
            return Fail("UnexpectedDemotions", "Demoting administrators is not required for this update. Remove demoteAdminUserIds or lower the limits first.");

        if (minDeactivate == 0 && deactivateIds.Count > 0)
            return Fail("UnexpectedDeactivations", "Deactivating users is not required for this update. Remove deactivateUserIds or lower the limits first.");

        if (minDemote > 0)
        {
            var err = await _limitUserOperations.DemoteAdminsAsync(bid, demoteIds, cancellationToken);
            if (err != null)
                return Fail("DemoteFailed", err);
        }

        if (minDeactivate > 0)
        {
            var err = await _limitUserOperations.DeactivateUsersAsync(bid, deactivateIds, cancellationToken);
            if (err != null)
                return Fail("DeactivateFailed", err);
        }

        if (request.MaxUserAccounts.HasValue)
            company.MaxUserAccounts = request.MaxUserAccounts;
        if (request.MaxAdminAccounts.HasValue)
            company.MaxAdminAccounts = request.MaxAdminAccounts;
        if (request.SubscriptionPlanId.HasValue)
            company.SubscriptionPlanId = request.SubscriptionPlanId;

        await _companies.UpdateAsync(company, cancellationToken);

        if (request.ResendAdminInvite)
        {
            var businessIdForInvite = company.BusinessId?.Trim();
            if (string.IsNullOrEmpty(businessIdForInvite))
                return Fail("BusinessIdRequired", "Company has no Business ID; cannot resend invite.");

            var adminsNow = await _metrics.CountAdminsForBusinessIdAsync(businessIdForInvite, cancellationToken);
            if (adminsNow > 0)
                return Fail("AlreadyHasAdmin", "Cannot resend invite: company already has an administrator.");

            var targets = CompanyAdminInviteEmailsHelper.GetExplicitTargets(company.InitialAdminInviteEmailsJson, company.InitialAdminInviteEmail);
            await _inviteIssuer.TryIssueInitialAdminInvitesAsync(
                company.Id,
                businessIdForInvite,
                targets.Count > 0 ? targets.ToList() : null,
                cancellationToken);
        }

        var c = await _companies.GetByIdAsync(request.CompanyId, cancellationToken);
        if (c == null)
            return Fail("NotFound", "Company not found after update.");

        var users = string.IsNullOrEmpty(bid) ? 0 : await _metrics.CountActiveUsersForBusinessIdAsync(bid, cancellationToken);
        var adminCount = string.IsNullOrEmpty(bid) ? 0 : await _metrics.CountAdminsForBusinessIdAsync(bid, cancellationToken);

        var inviteList = CompanyAdminInviteEmailsHelper.GetExplicitTargets(c.InitialAdminInviteEmailsJson, c.InitialAdminInviteEmail);

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
                AdminCount = adminCount,
                SubscriptionPlanId = c.SubscriptionPlanId
            }
        };
    }

    private static List<string> NormalizeIds(IReadOnlyList<string>? ids)
    {
        if (ids == null || ids.Count == 0)
            return new List<string>();
        return ids
            .Select(x => x?.Trim() ?? "")
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static AdminCompanyMutationResult LimitReductionConflict(
        string businessId,
        int currentActive,
        int? proposedMaxUsers,
        int currentAdmins,
        int? proposedMaxAdmins,
        int minDeactivate,
        int minDemote) =>
        new()
        {
            Success = false,
            ErrorCode = "LimitReductionRequired",
            Message =
                "Lower limits require demoting administrators and/or deactivating users. Retry the same request with demoteAdminUserIds and/or deactivateUserIds (see minimum counts in the response).",
            LimitReductionRequired = new LimitReductionRequiredDetails
            {
                BusinessId = businessId,
                ActiveUserCount = currentActive,
                ProposedMaxUserAccounts = proposedMaxUsers,
                AdminCount = currentAdmins,
                ProposedMaxAdminAccounts = proposedMaxAdmins,
                MinimumUsersToDeactivate = minDeactivate,
                MinimumAdminsToDemote = minDemote
            }
        };

    private static AdminCompanyMutationResult Fail(string code, string message) =>
        new() { Success = false, ErrorCode = code, Message = message };
}
