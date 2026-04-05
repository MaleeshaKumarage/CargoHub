namespace CargoHub.Application.Billing.AdminPlans;

public sealed class AdminSubscriptionPlanDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Kind { get; init; } = "";
    public string ChargeTimeAnchor { get; init; } = "";
    public int? TrialBookingAllowance { get; init; }
    public string Currency { get; init; } = "EUR";
    public bool IsActive { get; init; }
    public IReadOnlyList<AdminPricingPeriodDto> PricingPeriods { get; init; } = Array.Empty<AdminPricingPeriodDto>();
}

public sealed class AdminPricingPeriodDto
{
    public Guid Id { get; init; }
    public DateTime EffectiveFromUtc { get; init; }
    public decimal? ChargePerBooking { get; init; }
    public decimal? MonthlyFee { get; init; }
    public int? IncludedBookingsPerMonth { get; init; }
    public decimal? OverageChargePerBooking { get; init; }
    public IReadOnlyList<AdminPricingTierDto> Tiers { get; init; } = Array.Empty<AdminPricingTierDto>();
}

public sealed class AdminPricingTierDto
{
    public Guid Id { get; init; }
    public int Ordinal { get; init; }
    public int? InclusiveMaxBookingsInPeriod { get; init; }
    public decimal? ChargePerBooking { get; init; }
    public decimal? MonthlyFee { get; init; }
}
