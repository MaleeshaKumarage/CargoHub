using MediatR;

namespace CargoHub.Application.Billing.AdminPlans;

public sealed record ReplaceAdminPricingPeriodTiersCommand(
    Guid PeriodId,
    IReadOnlyList<AdminPricingTierInput> Tiers) : IRequest<AdminPlanMutationResult>;

public sealed class ReplaceAdminPricingPeriodTiersCommandHandler
    : IRequestHandler<ReplaceAdminPricingPeriodTiersCommand, AdminPlanMutationResult>
{
    private readonly ISubscriptionPlanAdminRepository _repo;

    public ReplaceAdminPricingPeriodTiersCommandHandler(ISubscriptionPlanAdminRepository repo) => _repo = repo;

    public Task<AdminPlanMutationResult> Handle(
        ReplaceAdminPricingPeriodTiersCommand request,
        CancellationToken cancellationToken) =>
        _repo.ReplaceTiersAsync(request.PeriodId, request.Tiers, cancellationToken);
}
