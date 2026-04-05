namespace CargoHub.Application.Billing;

/// <summary>Confirms billable transitions and posts subscription billing lines.</summary>
public interface ISubscriptionBillingOrchestrator
{
    /// <summary>
    /// Confirm draft in one DB transaction: trial check, FirstBillableAtUtc, lines, status history.
    /// Returns false if the draft does not exist or is not owned by the customer.
    /// </summary>
    /// <exception cref="SubscriptionBillingException">Trial allowance exhausted, etc.</exception>
    Task<bool> ConfirmDraftWithBillingAsync(Guid bookingId, string customerId, CancellationToken cancellationToken = default);

    /// <summary>Throws if the company cannot add another billable (non-test) booking under the current trial.</summary>
    /// <exception cref="SubscriptionBillingException">Trial allowance exhausted.</exception>
    Task AssertBillableBookingAllowedAsync(Guid? companyId, bool isTestBooking, CancellationToken cancellationToken = default);

    /// <summary>After a completed booking is inserted, set FirstBillableAtUtc and post billing lines.</summary>
    Task PostBillingForNewCompletedBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);
}
