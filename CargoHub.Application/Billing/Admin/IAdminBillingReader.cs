namespace CargoHub.Application.Billing.Admin;

public interface IAdminBillingReader
{
    Task<IReadOnlyList<AdminSubscriptionPlanSummaryDto>> ListSubscriptionPlansAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyBillingPeriodSummaryDto>> ListBillingPeriodsForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<BillingPeriodDetailDto?> GetBillingPeriodDetailAsync(Guid periodId, CancellationToken cancellationToken = default);

    Task<BillingInvoicePdfModel?> GetInvoicePdfModelAsync(Guid periodId, CancellationToken cancellationToken = default);
}
