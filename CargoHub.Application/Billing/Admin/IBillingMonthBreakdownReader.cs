namespace CargoHub.Application.Billing.Admin;

public interface IBillingMonthBreakdownReader
{
    Task<IReadOnlyList<BillableMonthSummaryDto>> GetBillableMonthsAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<BillingMonthBreakdownDto?> GetBreakdownAsync(Guid companyId, int yearUtc, int monthUtc, CancellationToken cancellationToken = default);
}
