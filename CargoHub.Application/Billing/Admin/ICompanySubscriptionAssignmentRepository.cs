namespace CargoHub.Application.Billing.Admin;

public interface ICompanySubscriptionAssignmentRepository
{
    Task RecordAsync(
        Guid companyId,
        Guid subscriptionPlanId,
        DateTime effectiveFromUtc,
        string? setByUserId,
        CancellationToken cancellationToken = default);
}
