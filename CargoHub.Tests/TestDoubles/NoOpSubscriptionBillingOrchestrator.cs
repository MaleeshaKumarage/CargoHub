using CargoHub.Application.Billing;

namespace CargoHub.Tests.TestDoubles;

internal sealed class NoOpSubscriptionBillingOrchestrator : ISubscriptionBillingOrchestrator
{
    public Task<bool> ConfirmDraftWithBillingAsync(Guid bookingId, string customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task AssertBillableBookingAllowedAsync(Guid? companyId, bool isTestBooking, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task PostBillingForNewCompletedBookingAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
