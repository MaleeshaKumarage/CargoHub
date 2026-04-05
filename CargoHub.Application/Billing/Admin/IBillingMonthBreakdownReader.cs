namespace CargoHub.Application.Billing.Admin;

public interface IBillingMonthBreakdownReader
{
    Task<IReadOnlyList<BillableMonthSummaryDto>> GetBillableMonthsAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<BillingMonthBreakdownDto?> GetBreakdownAsync(Guid companyId, int yearUtc, int monthUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Billable bookings whose first billable instant falls in <c>[rangeStartUtc, rangeEndExclusiveUtc)</c> (UTC).
    /// <see cref="BillingMonthBreakdownDto.BillingPeriodId"/> is set only when all matching bookings fall in one UTC calendar month.
    /// </summary>
    Task<BillingMonthBreakdownDto?> GetBreakdownForDateRangeAsync(
        Guid companyId,
        DateTime rangeStartUtc,
        DateTime rangeEndExclusiveUtc,
        CancellationToken cancellationToken = default);
}
