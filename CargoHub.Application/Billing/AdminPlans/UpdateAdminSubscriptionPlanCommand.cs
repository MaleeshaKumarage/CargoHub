using MediatR;

namespace CargoHub.Application.Billing.AdminPlans;

public sealed record UpdateAdminSubscriptionPlanCommand(
    Guid PlanId,
    string Name,
    string Kind,
    string ChargeTimeAnchor,
    int? TrialBookingAllowance,
    string Currency,
    bool IsActive) : IRequest<AdminPlanMutationResult>;

public sealed class UpdateAdminSubscriptionPlanCommandHandler
    : IRequestHandler<UpdateAdminSubscriptionPlanCommand, AdminPlanMutationResult>
{
    private readonly ISubscriptionPlanAdminRepository _repo;

    public UpdateAdminSubscriptionPlanCommandHandler(ISubscriptionPlanAdminRepository repo) => _repo = repo;

    public async Task<AdminPlanMutationResult> Handle(
        UpdateAdminSubscriptionPlanCommand request,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
            return AdminPlanMutationResult.Fail("NameRequired", "Plan name is required.");

        if (!Enum.TryParse<Domain.Billing.SubscriptionPlanKind>(request.Kind, true, out var kind))
            return AdminPlanMutationResult.Fail("InvalidKind", "Invalid subscription plan kind.");

        if (!Enum.TryParse<Domain.Billing.ChargeTimeAnchor>(request.ChargeTimeAnchor, true, out var anchor))
            return AdminPlanMutationResult.Fail("InvalidAnchor", "Invalid charge time anchor.");

        if (kind == Domain.Billing.SubscriptionPlanKind.Trial &&
            (!request.TrialBookingAllowance.HasValue || request.TrialBookingAllowance < 1))
            return AdminPlanMutationResult.Fail("TrialAllowanceRequired", "Trial plans require a positive trial booking allowance.");

        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency.Trim().ToUpperInvariant();
        if (currency.Length != 3)
            return AdminPlanMutationResult.Fail("InvalidCurrency", "Currency must be a 3-letter ISO code.");

        return await _repo.UpdatePlanAsync(
            request.PlanId,
            name,
            kind.ToString(),
            anchor.ToString(),
            kind == Domain.Billing.SubscriptionPlanKind.Trial ? request.TrialBookingAllowance : null,
            currency,
            request.IsActive,
            cancellationToken);
    }
}
