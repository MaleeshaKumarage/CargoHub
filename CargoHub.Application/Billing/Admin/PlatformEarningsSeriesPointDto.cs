namespace CargoHub.Application.Billing.Admin;

/// <summary>One bucket for platform earnings chart: <see cref="Period"/> is yyyy-MM-dd (daily) or yyyy-MM (monthly).</summary>
public sealed class PlatformEarningsSeriesPointDto
{
    public string Period { get; init; } = "";

    public decimal TotalEur { get; init; }
}
