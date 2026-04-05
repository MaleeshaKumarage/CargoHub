namespace CargoHub.Application.Subscriptions;

/// <summary>Portal-facing subscription snapshot for the current company (rates from the active pricing period at UTC now).</summary>
public sealed class PortalCompanySubscriptionDto
{
    public string PlanName { get; init; } = "";

    /// <summary>Subscription plan kind name (matches <c>SubscriptionPlanKind</c> enum).</summary>
    public string PlanKind { get; init; } = "";

    public string Currency { get; init; } = "EUR";

    public int? TrialBookingAllowance { get; init; }

    public decimal? ChargePerBooking { get; init; }

    public decimal? MonthlyFee { get; init; }

    public int? IncludedBookingsPerMonth { get; init; }

    public decimal? OverageChargePerBooking { get; init; }

    public IReadOnlyList<PortalSubscriptionTierDto>? Tiers { get; init; }
}

public sealed class PortalSubscriptionTierDto
{
    public int Ordinal { get; init; }

    public int? InclusiveMaxBookingsInPeriod { get; init; }

    public decimal? ChargePerBooking { get; init; }

    public decimal? MonthlyFee { get; init; }
}
