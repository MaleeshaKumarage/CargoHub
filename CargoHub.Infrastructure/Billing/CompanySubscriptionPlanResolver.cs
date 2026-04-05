using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Billing;

/// <summary>
/// Resolves which subscription plan applies to a company at a given instant (assignment history, then the company row's subscription plan id).
/// </summary>
public static class CompanySubscriptionPlanResolver
{
    public static async Task<Guid?> ResolvePlanIdAtAsync(
        ApplicationDbContext db,
        Guid companyId,
        DateTime instantUtc,
        CancellationToken cancellationToken = default)
    {
        var row = await db.CompanySubscriptionAssignments.AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.EffectiveFromUtc <= instantUtc)
            .OrderByDescending(a => a.EffectiveFromUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (row != null)
            return row.SubscriptionPlanId;

        return await db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => c.SubscriptionPlanId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
