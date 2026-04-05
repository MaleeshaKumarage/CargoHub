using MediatR;

namespace CargoHub.Application.Billing.Admin;

public sealed record SetBillingPeriodBookingExcludedCommand(
    Guid CompanyBillingPeriodId,
    Guid BookingId,
    bool Excluded,
    string? SuperAdminUserId) : IRequest<BillingPeriodBookingExclusionResult>;

public sealed class SetBillingPeriodBookingExcludedCommandHandler
    : IRequestHandler<SetBillingPeriodBookingExcludedCommand, BillingPeriodBookingExclusionResult>
{
    private readonly IBillingPeriodBookingExclusionMutator _mutator;

    public SetBillingPeriodBookingExcludedCommandHandler(IBillingPeriodBookingExclusionMutator mutator) =>
        _mutator = mutator;

    public Task<BillingPeriodBookingExclusionResult> Handle(
        SetBillingPeriodBookingExcludedCommand request,
        CancellationToken cancellationToken) =>
        _mutator.SetExcludedAsync(
            request.CompanyBillingPeriodId,
            request.BookingId,
            request.Excluded,
            request.SuperAdminUserId,
            cancellationToken);
}
