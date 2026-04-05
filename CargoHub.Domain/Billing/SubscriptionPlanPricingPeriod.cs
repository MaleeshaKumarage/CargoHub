namespace CargoHub.Domain.Billing;

/// <summary>Versioned rate card for a plan; append-only effective-dated rows.</summary>
public class SubscriptionPlanPricingPeriod
{
    public Guid Id { get; set; }

    public Guid SubscriptionPlanId { get; set; }

    public SubscriptionPlan? SubscriptionPlan { get; set; }

    /// <summary>UTC inclusive; latest row with EffectiveFromUtc &lt;= anchor instant wins.</summary>
    public DateTime EffectiveFromUtc { get; set; }

    /// <summary>Flat pay-per-booking (simple kind).</summary>
    public decimal? ChargePerBooking { get; set; }

    public decimal? MonthlyFee { get; set; }

    public int? IncludedBookingsPerMonth { get; set; }

    public decimal? OverageChargePerBooking { get; set; }

    public ICollection<SubscriptionPlanPricingTier> Tiers { get; set; } = new List<SubscriptionPlanPricingTier>();
}
