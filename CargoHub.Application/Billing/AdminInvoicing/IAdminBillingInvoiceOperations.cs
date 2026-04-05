namespace CargoHub.Application.Billing.AdminInvoicing;

public interface IAdminBillingInvoiceOperations
{
    Task<SendInvoiceEmailResult> SendInvoiceEmailAsync(
        Guid periodId,
        string recipientAdminUserId,
        string sentBySuperAdminUserId,
        CancellationToken cancellationToken = default);

    Task<UpdateLineExcludedResult> UpdateLineExcludedAsync(
        Guid lineItemId,
        bool excludedFromInvoice,
        string superAdminUserId,
        CancellationToken cancellationToken = default);
}
