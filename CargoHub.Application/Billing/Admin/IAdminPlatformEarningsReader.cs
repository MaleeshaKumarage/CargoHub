namespace CargoHub.Application.Billing.Admin;

public interface IAdminPlatformEarningsReader
{
    /// <summary>Daily or monthly EUR buckets for the platform earnings chart (invoice-eligible EUR lines).</summary>
    Task<IReadOnlyList<PlatformEarningsSeriesPointDto>> GetSeriesAsync(
        PlatformEarningsSeriesRange range,
        CancellationToken cancellationToken = default);

    /// <summary>Last <paramref name="months"/> UTC months including current; months with no lines show 0.</summary>
    Task<IReadOnlyList<PlatformEarningsMonthDto>> GetMonthlyTotalsAsync(int months, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformEarningsCompanyDto>> GetByCompanyForMonthAsync(
        int yearUtc,
        int monthUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformEarningsSubscriptionDto>> GetBySubscriptionForMonthAsync(
        int yearUtc,
        int monthUtc,
        CancellationToken cancellationToken = default);
}
