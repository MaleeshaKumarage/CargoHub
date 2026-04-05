using CargoHub.Application.Billing.AdminPlans;
using CargoHub.Domain.Billing;
using CargoHub.Domain.Companies;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Persistence;

public sealed class SubscriptionPlanAdminRepository : ISubscriptionPlanAdminRepository
{
    private readonly ApplicationDbContext _db;

    public SubscriptionPlanAdminRepository(ApplicationDbContext db) => _db = db;

    public async Task<AdminSubscriptionPlanDetailDto?> GetPlanDetailAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await _db.SubscriptionPlans.AsNoTracking()
            .Include(p => p.PricingPeriods)
            .ThenInclude(pp => pp.Tiers)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        if (plan == null)
            return null;

        var periods = plan.PricingPeriods
            .OrderByDescending(pp => pp.EffectiveFromUtc)
            .Select(pp => new AdminPricingPeriodDto
            {
                Id = pp.Id,
                EffectiveFromUtc = pp.EffectiveFromUtc,
                ChargePerBooking = pp.ChargePerBooking,
                MonthlyFee = pp.MonthlyFee,
                IncludedBookingsPerMonth = pp.IncludedBookingsPerMonth,
                OverageChargePerBooking = pp.OverageChargePerBooking,
                Tiers = pp.Tiers.OrderBy(t => t.Ordinal).Select(t => new AdminPricingTierDto
                {
                    Id = t.Id,
                    Ordinal = t.Ordinal,
                    InclusiveMaxBookingsInPeriod = t.InclusiveMaxBookingsInPeriod,
                    ChargePerBooking = t.ChargePerBooking,
                    MonthlyFee = t.MonthlyFee
                }).ToList()
            }).ToList();

        return new AdminSubscriptionPlanDetailDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Kind = plan.Kind.ToString(),
            ChargeTimeAnchor = plan.ChargeTimeAnchor.ToString(),
            TrialBookingAllowance = plan.TrialBookingAllowance,
            Currency = plan.Currency,
            IsActive = plan.IsActive,
            PricingPeriods = periods
        };
    }

    public Task<bool> PlanExistsAsync(Guid planId, CancellationToken cancellationToken = default) =>
        _db.SubscriptionPlans.AnyAsync(p => p.Id == planId, cancellationToken);

    public Task<int> CountCompaniesUsingPlanAsync(Guid planId, CancellationToken cancellationToken = default) =>
        _db.Companies.CountAsync(c => c.SubscriptionPlanId == planId, cancellationToken);

    public async Task<Guid> CreatePlanAsync(
        string name,
        string kind,
        string chargeTimeAnchor,
        int? trialBookingAllowance,
        string currency,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = name,
            Kind = Enum.Parse<SubscriptionPlanKind>(kind),
            ChargeTimeAnchor = Enum.Parse<ChargeTimeAnchor>(chargeTimeAnchor),
            TrialBookingAllowance = trialBookingAllowance,
            Currency = currency,
            IsActive = isActive
        };
        _db.SubscriptionPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);
        return plan.Id;
    }

    public async Task<AdminPlanMutationResult> UpdatePlanAsync(
        Guid planId,
        string name,
        string kind,
        string chargeTimeAnchor,
        int? trialBookingAllowance,
        string currency,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        if (plan == null)
            return AdminPlanMutationResult.Fail("NotFound", "Subscription plan not found.");

        if (!Enum.TryParse<SubscriptionPlanKind>(kind, true, out var kindEnum))
            return AdminPlanMutationResult.Fail("InvalidKind", "Invalid subscription plan kind.");
        if (!Enum.TryParse<ChargeTimeAnchor>(chargeTimeAnchor, true, out var anchorEnum))
            return AdminPlanMutationResult.Fail("InvalidAnchor", "Invalid charge time anchor.");
        if (kindEnum == SubscriptionPlanKind.Trial && (!trialBookingAllowance.HasValue || trialBookingAllowance < 1))
            return AdminPlanMutationResult.Fail("TrialAllowanceRequired", "Trial plans require a positive trial booking allowance.");

        plan.Name = name.Trim();
        plan.Kind = kindEnum;
        plan.ChargeTimeAnchor = anchorEnum;
        plan.TrialBookingAllowance = kindEnum == SubscriptionPlanKind.Trial ? trialBookingAllowance : null;
        plan.Currency = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim().ToUpperInvariant();
        plan.IsActive = isActive;

        await _db.SaveChangesAsync(cancellationToken);
        return AdminPlanMutationResult.Ok();
    }

    public async Task<AdminPlanMutationResult> DeletePlanAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var companies = await CountCompaniesUsingPlanAsync(planId, cancellationToken);
        if (companies > 0)
            return AdminPlanMutationResult.Fail("PlanInUse", "Cannot delete: one or more companies are assigned to this plan.");

        var billingLines = await _db.BillingLineItems.AnyAsync(l => l.SubscriptionPlanId == planId, cancellationToken);
        if (billingLines)
            return AdminPlanMutationResult.Fail("PlanHasBillingHistory", "Cannot delete: billing line items reference this plan.");

        var plan = await _db.SubscriptionPlans
            .Include(p => p.PricingPeriods)
            .ThenInclude(pp => pp.Tiers)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        if (plan == null)
            return AdminPlanMutationResult.Fail("NotFound", "Subscription plan not found.");

        foreach (var pp in plan.PricingPeriods)
            _db.SubscriptionPlanPricingTiers.RemoveRange(pp.Tiers);
        _db.SubscriptionPlanPricingPeriods.RemoveRange(plan.PricingPeriods);
        _db.SubscriptionPlans.Remove(plan);
        await _db.SaveChangesAsync(cancellationToken);
        return AdminPlanMutationResult.Ok();
    }

    public async Task<AdminPlanMutationResult> AddPricingPeriodAsync(
        Guid planId,
        DateTime effectiveFromUtc,
        decimal? chargePerBooking,
        decimal? monthlyFee,
        int? includedBookingsPerMonth,
        decimal? overageChargePerBooking,
        CancellationToken cancellationToken = default)
    {
        if (!await PlanExistsAsync(planId, cancellationToken))
            return AdminPlanMutationResult.Fail("NotFound", "Subscription plan not found.");

        var period = new SubscriptionPlanPricingPeriod
        {
            Id = Guid.NewGuid(),
            SubscriptionPlanId = planId,
            EffectiveFromUtc = effectiveFromUtc.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(effectiveFromUtc, DateTimeKind.Utc)
                : effectiveFromUtc.ToUniversalTime(),
            ChargePerBooking = chargePerBooking,
            MonthlyFee = monthlyFee,
            IncludedBookingsPerMonth = includedBookingsPerMonth,
            OverageChargePerBooking = overageChargePerBooking
        };
        _db.SubscriptionPlanPricingPeriods.Add(period);
        await _db.SaveChangesAsync(cancellationToken);
        return AdminPlanMutationResult.Ok();
    }

    public async Task<AdminPlanMutationResult> UpdatePricingPeriodAsync(
        Guid periodId,
        DateTime effectiveFromUtc,
        decimal? chargePerBooking,
        decimal? monthlyFee,
        int? includedBookingsPerMonth,
        decimal? overageChargePerBooking,
        CancellationToken cancellationToken = default)
    {
        var period = await _db.SubscriptionPlanPricingPeriods.FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken);
        if (period == null)
            return AdminPlanMutationResult.Fail("NotFound", "Pricing period not found.");

        period.EffectiveFromUtc = effectiveFromUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(effectiveFromUtc, DateTimeKind.Utc)
            : effectiveFromUtc.ToUniversalTime();
        period.ChargePerBooking = chargePerBooking;
        period.MonthlyFee = monthlyFee;
        period.IncludedBookingsPerMonth = includedBookingsPerMonth;
        period.OverageChargePerBooking = overageChargePerBooking;
        await _db.SaveChangesAsync(cancellationToken);
        return AdminPlanMutationResult.Ok();
    }

    public async Task<AdminPlanMutationResult> DeletePricingPeriodAsync(Guid periodId, CancellationToken cancellationToken = default)
    {
        var referenced = await _db.BillingLineItems.AnyAsync(l => l.SubscriptionPlanPricingPeriodId == periodId, cancellationToken);
        if (referenced)
            return AdminPlanMutationResult.Fail("PeriodInUse", "Cannot delete: billing lines reference this pricing period.");

        var period = await _db.SubscriptionPlanPricingPeriods
            .Include(p => p.Tiers)
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken);
        if (period == null)
            return AdminPlanMutationResult.Fail("NotFound", "Pricing period not found.");

        _db.SubscriptionPlanPricingTiers.RemoveRange(period.Tiers);
        _db.SubscriptionPlanPricingPeriods.Remove(period);
        await _db.SaveChangesAsync(cancellationToken);
        return AdminPlanMutationResult.Ok();
    }

    public async Task<AdminPlanMutationResult> ReplaceTiersAsync(
        Guid periodId,
        IReadOnlyList<AdminPricingTierInput> tiers,
        CancellationToken cancellationToken = default)
    {
        var period = await _db.SubscriptionPlanPricingPeriods
            .Include(p => p.Tiers)
            .FirstOrDefaultAsync(p => p.Id == periodId, cancellationToken);
        if (period == null)
            return AdminPlanMutationResult.Fail("NotFound", "Pricing period not found.");

        var ordinals = tiers.Select(t => t.Ordinal).ToList();
        if (ordinals.Count != ordinals.Distinct().Count())
            return AdminPlanMutationResult.Fail("DuplicateOrdinal", "Tier ordinals must be unique.");

        _db.SubscriptionPlanPricingTiers.RemoveRange(period.Tiers);
        foreach (var t in tiers.OrderBy(x => x.Ordinal))
        {
            _db.SubscriptionPlanPricingTiers.Add(new SubscriptionPlanPricingTier
            {
                Id = Guid.NewGuid(),
                SubscriptionPlanPricingPeriodId = periodId,
                Ordinal = t.Ordinal,
                InclusiveMaxBookingsInPeriod = t.InclusiveMaxBookingsInPeriod,
                ChargePerBooking = t.ChargePerBooking,
                MonthlyFee = t.MonthlyFee
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return AdminPlanMutationResult.Ok();
    }
}
