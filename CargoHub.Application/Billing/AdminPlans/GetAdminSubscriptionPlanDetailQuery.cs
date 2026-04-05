using MediatR;

namespace CargoHub.Application.Billing.AdminPlans;

public sealed record GetAdminSubscriptionPlanDetailQuery(Guid PlanId) : IRequest<AdminSubscriptionPlanDetailDto?>;

public sealed class GetAdminSubscriptionPlanDetailQueryHandler : IRequestHandler<GetAdminSubscriptionPlanDetailQuery, AdminSubscriptionPlanDetailDto?>
{
    private readonly ISubscriptionPlanAdminRepository _repo;

    public GetAdminSubscriptionPlanDetailQueryHandler(ISubscriptionPlanAdminRepository repo) => _repo = repo;

    public Task<AdminSubscriptionPlanDetailDto?> Handle(GetAdminSubscriptionPlanDetailQuery request, CancellationToken cancellationToken) =>
        _repo.GetPlanDetailAsync(request.PlanId, cancellationToken);
}
