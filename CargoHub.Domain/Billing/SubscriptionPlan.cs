namespace CargoHub.Domain.Billing;

/// <summary>
/// Named subscription template (trial, paygo, tiered, monthly variants). Rate rows live in <see cref="SubscriptionPlanPricingPeriod"/>.
/// </summary>
public class SubscriptionPlan
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public SubscriptionPlanKind Kind { get; set; }

    /// <summary>Which booking timestamp drives period resolution and volume sort (when applicable).</summary>
    public ChargeTimeAnchor ChargeTimeAnchor { get; set; } = ChargeTimeAnchor.FirstBillableAtUtc;

    /// <summary>For <see cref="SubscriptionPlanKind.Trial"/>; ignored for other kinds.</summary>
    public int? TrialBookingAllowance { get; set; }

    /// <summary>ISO 4217 (e.g. EUR).</summary>
    public string Currency { get; set; } = "EUR";

    public bool IsActive { get; set; } = true;

    public ICollection<SubscriptionPlanPricingPeriod> PricingPeriods { get; set; } = new List<SubscriptionPlanPricingPeriod>();
}
