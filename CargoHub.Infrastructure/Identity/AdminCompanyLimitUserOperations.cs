using CargoHub.Application.AdminCompanies;
using CargoHub.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace CargoHub.Infrastructure.Identity;

public sealed class AdminCompanyLimitUserOperations : IAdminCompanyLimitUserOperations
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AdminCompanyUserPolicy _policy;

    public AdminCompanyLimitUserOperations(UserManager<ApplicationUser> userManager, AdminCompanyUserPolicy policy)
    {
        _userManager = userManager;
        _policy = policy;
    }

    public async Task<string?> DemoteAdminsAsync(string businessId, IReadOnlyList<string> userIds, CancellationToken cancellationToken = default)
    {
        var bid = businessId?.Trim() ?? "";
        if (string.IsNullOrEmpty(bid))
            return "Business ID is required.";

        foreach (var id in DistinctNonEmpty(userIds))
        {
            var u = await _userManager.FindByIdAsync(id);
            if (u == null)
                return "One or more users were not found.";
            if (!SameBusiness(u.BusinessId, bid))
                return $"User {u.Email ?? id} does not belong to this company.";
            if (await _userManager.IsInRoleAsync(u, RoleNames.SuperAdmin))
                return "SuperAdmin accounts cannot be changed from company settings.";
            if (!await _userManager.IsInRoleAsync(u, RoleNames.Admin))
                return $"User {u.Email ?? id} is not an administrator.";
            var err = await _policy.ValidatePatchAsync(u, RoleNames.User, null, cancellationToken);
            if (err != null)
                return err;
            var roles = await _userManager.GetRolesAsync(u);
            await _userManager.RemoveFromRolesAsync(u, roles);
            await _userManager.AddToRoleAsync(u, RoleNames.User);
        }

        return null;
    }

    public async Task<string?> DeactivateUsersAsync(string businessId, IReadOnlyList<string> userIds, CancellationToken cancellationToken = default)
    {
        var bid = businessId?.Trim() ?? "";
        if (string.IsNullOrEmpty(bid))
            return "Business ID is required.";

        foreach (var id in DistinctNonEmpty(userIds))
        {
            var u = await _userManager.FindByIdAsync(id);
            if (u == null)
                return "One or more users were not found.";
            if (!SameBusiness(u.BusinessId, bid))
                return $"User {u.Email ?? id} does not belong to this company.";
            if (await _userManager.IsInRoleAsync(u, RoleNames.SuperAdmin))
                return "SuperAdmin accounts cannot be deactivated from company settings.";
            if (!u.IsActive)
                continue;
            var err = await _policy.ValidatePatchAsync(u, null, false, cancellationToken);
            if (err != null)
                return err;
            u.IsActive = false;
            await _userManager.UpdateAsync(u);
        }

        return null;
    }

    private static IEnumerable<string> DistinctNonEmpty(IReadOnlyList<string> userIds)
    {
        return userIds
            .Select(x => x?.Trim() ?? "")
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.Ordinal);
    }

    private static bool SameBusiness(string? userBusinessId, string expectedNormalized)
    {
        var e = expectedNormalized.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(userBusinessId))
            return false;
        return userBusinessId.Trim().ToLowerInvariant() == e;
    }
}
