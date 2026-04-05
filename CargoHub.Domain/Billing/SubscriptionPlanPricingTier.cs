namespace CargoHub.Domain.Billing;

/// <summary>Volume band within a pricing period (tiered paygo or tiered monthly-by-usage).</summary>
public class SubscriptionPlanPricingTier
{
    public Guid Id { get; set; }

    public Guid SubscriptionPlanPricingPeriodId { get; set; }

    public SubscriptionPlanPricingPeriod? PricingPeriod { get; set; }

    public int Ordinal { get; set; }

    /// <summary>Null = open-ended final tier.</summary>
    public int? InclusiveMaxBookingsInPeriod { get; set; }

    public decimal? ChargePerBooking { get; set; }

    public decimal? MonthlyFee { get; set; }
}
