namespace CargoHub.Application.Billing.Admin;

/// <summary>Time window for platform earnings line chart (UTC).</summary>
public enum PlatformEarningsSeriesRange
{
    Yesterday = 0,
    Last7Days = 1,
    LastMonth = 2,
    Last6Months = 3,
    LastYear = 4,
}
