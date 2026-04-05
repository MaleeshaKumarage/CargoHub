using CargoHub.Application.Subscriptions;
using CargoHub.Domain.Billing;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Persistence;

public sealed class PortalCompanySubscriptionReader : IPortalCompanySubscriptionReader
{
    private readonly ApplicationDbContext _db;

    public PortalCompanySubscriptionReader(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PortalCompanySubscriptionDto?> GetForBusinessIdAsync(string businessId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(businessId))
            return null;

        var bid = businessId.Trim();
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.BusinessId != null && c.BusinessId.Trim().ToLower() == bid.ToLowerInvariant(),
                cancellationToken);
        if (company == null)
            return null;

        if (company.SubscriptionPlanId is not { } planId)
        {
            return new PortalCompanySubscriptionDto
            {
                PlanName = "",
                PlanKind = "None",
                Currency = "EUR"
            };
        }

        var plan = await _db.SubscriptionPlans.AsNoTracking()
            .Include(p => p.PricingPeriods)
            .ThenInclude(pp => pp.Tiers)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

        if (plan == null)
        {
            return new PortalCompanySubscriptionDto
            {
                PlanName = "",
                PlanKind = "Unknown",
                Currency = "EUR"
            };
        }

        var instant = DateTime.UtcNow;
        var period = plan.PricingPeriods
            .Where(pp => pp.EffectiveFromUtc <= instant)
            .OrderByDescending(pp => pp.EffectiveFromUtc)
            .FirstOrDefault();

        IReadOnlyList<PortalSubscriptionTierDto>? tiers = null;
        if (period?.Tiers is { Count: > 0 } t)
        {
            tiers = t.OrderBy(x => x.Ordinal)
                .Select(x => new PortalSubscriptionTierDto
                {
                    Ordinal = x.Ordinal,
                    InclusiveMaxBookingsInPeriod = x.InclusiveMaxBookingsInPeriod,
                    ChargePerBooking = x.ChargePerBooking,
                    MonthlyFee = x.MonthlyFee
                })
                .ToList();
        }

        return new PortalCompanySubscriptionDto
        {
            PlanName = plan.Name,
            PlanKind = plan.Kind.ToString(),
            Currency = plan.Currency,
            TrialBookingAllowance = plan.Kind == SubscriptionPlanKind.Trial ? plan.TrialBookingAllowance : null,
            ChargePerBooking = period?.ChargePerBooking,
            MonthlyFee = period?.MonthlyFee,
            IncludedBookingsPerMonth = period?.IncludedBookingsPerMonth,
            OverageChargePerBooking = period?.OverageChargePerBooking,
            Tiers = tiers
        };
    }
}
