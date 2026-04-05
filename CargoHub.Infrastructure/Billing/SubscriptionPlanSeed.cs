using CargoHub.Application.Billing;
using CargoHub.Domain.Billing;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Billing;

public static class SubscriptionPlanSeed
{
    public static async Task EnsureDefaultTrialPlanAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var id = SubscriptionBillingConstants.DefaultTrialPlanId;
        if (await db.SubscriptionPlans.AnyAsync(p => p.Id == id, cancellationToken))
            return;

        db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = id,
            Name = "Trial",
            Kind = SubscriptionPlanKind.Trial,
            TrialBookingAllowance = 5,
            Currency = "EUR",
            IsActive = true,
            ChargeTimeAnchor = ChargeTimeAnchor.FirstBillableAtUtc
        });

        db.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = Guid.NewGuid(),
            SubscriptionPlanId = id,
            EffectiveFromUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
