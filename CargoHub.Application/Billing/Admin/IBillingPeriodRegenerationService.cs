namespace CargoHub.Application.Billing.Admin;

public interface IBillingPeriodRegenerationService
{
    /// <summary>Rebuild line items from billable bookings in the period month, honoring exclusion rows and subscription history.</summary>
    Task RegenerateAsync(Guid companyBillingPeriodId, CancellationToken cancellationToken = default);
}
