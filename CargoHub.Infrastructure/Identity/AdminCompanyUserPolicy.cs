using CargoHub.Application.Auth;
using CargoHub.Application.Company;
using CargoHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CargoHub.Infrastructure.Identity;

/// <summary>
/// Enforces max admins and last-admin rules for company-linked users.
/// </summary>
public sealed class AdminCompanyUserPolicy
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICompanyRepository _companies;
    private readonly ICompanyUserMetrics _metrics;

    public AdminCompanyUserPolicy(
        UserManager<ApplicationUser> userManager,
        ICompanyRepository companies,
        ICompanyUserMetrics metrics)
    {
        _userManager = userManager;
        _companies = companies;
        _metrics = metrics;
    }

    /// <summary>Null if allowed; otherwise error message for HTTP 400.</summary>
    public async Task<string?> ValidatePatchAsync(
        ApplicationUser user,
        string? requestedRole,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        if (await _userManager.IsInRoleAsync(user, RoleNames.SuperAdmin))
            return null;

        var businessId = user.BusinessId?.Trim();
        if (string.IsNullOrEmpty(businessId))
            return null;

        var company = await _companies.GetByBusinessIdAsync(businessId, cancellationToken);
        var isAdmin = await _userManager.IsInRoleAsync(user, RoleNames.Admin);

        if (isActive == false && isAdmin)
        {
            var admins = await _metrics.CountAdminsForBusinessIdAsync(businessId, cancellationToken);
            if (admins <= 1)
                return "Cannot deactivate the last company administrator.";
        }

        if (requestedRole == null)
            return null;

        if (requestedRole == RoleNames.Admin)
        {
            if (company?.MaxAdminAccounts is { } cap)
            {
                var admins = await _metrics.CountAdminsForBusinessIdAsync(businessId, cancellationToken);
                if (!isAdmin && admins >= cap)
                    return "This company has reached its administrator limit.";
            }

            return null;
        }

        if (requestedRole == RoleNames.User && isAdmin)
        {
            var admins = await _metrics.CountAdminsForBusinessIdAsync(businessId, cancellationToken);
            if (admins <= 1)
                return "Cannot remove the last company administrator. Promote another user first.";
        }

        return null;
    }
}
