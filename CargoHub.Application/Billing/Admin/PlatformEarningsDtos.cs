namespace CargoHub.Application.Billing.Admin;

/// <summary>Platform-wide payable total for one UTC calendar month (EUR line items only).</summary>
public sealed class PlatformEarningsMonthDto
{
    public int YearUtc { get; init; }

    public int MonthUtc { get; init; }

    public decimal TotalEur { get; init; }
}

/// <summary>One company's share of platform earnings for a UTC month.</summary>
public sealed class PlatformEarningsCompanyDto
{
    public Guid CompanyId { get; init; }

    public string CompanyName { get; init; } = "";

    public decimal AmountEur { get; init; }
}

/// <summary>Payable EUR attributed to a subscription plan for a UTC month (from line items).</summary>
public sealed class PlatformEarningsSubscriptionDto
{
    public Guid PlanId { get; init; }

    public string PlanName { get; init; } = "";

    public decimal AmountEur { get; init; }

    /// <summary>Percent of total payable EUR for the month (0–100).</summary>
    public decimal Percent { get; init; }
}
