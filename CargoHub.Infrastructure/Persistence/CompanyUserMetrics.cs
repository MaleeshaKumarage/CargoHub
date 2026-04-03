using CargoHub.Application.Auth;
using CargoHub.Application.Company;
using CargoHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Persistence;

public sealed class CompanyUserMetrics : ICompanyUserMetrics
{
    private readonly ApplicationDbContext _db;

    public CompanyUserMetrics(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<int> CountActiveUsersForBusinessIdAsync(string businessId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(businessId))
            return Task.FromResult(0);
        var n = businessId.Trim().ToLowerInvariant();
        return _db.Users.CountAsync(
            u => u.BusinessId != null && u.BusinessId.Trim().ToLower() == n && u.IsActive,
            cancellationToken);
    }

    public async Task<int> CountAdminsForBusinessIdAsync(string businessId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(businessId))
            return 0;
        var n = businessId.Trim().ToLowerInvariant();
        var adminRoleId = await _db.Set<IdentityRole>()
            .Where(r => r.NormalizedName == RoleNames.Admin.ToUpperInvariant())
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrEmpty(adminRoleId))
            return 0;

        return await (
            from u in _db.Users
            join ur in _db.Set<IdentityUserRole<string>>() on u.Id equals ur.UserId
            where ur.RoleId == adminRoleId
                  && u.BusinessId != null
                  && u.BusinessId.Trim().ToLower() == n
                  && u.IsActive
            select u.Id).CountAsync(cancellationToken);
    }
}
