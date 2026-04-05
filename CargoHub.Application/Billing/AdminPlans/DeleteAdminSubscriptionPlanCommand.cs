using MediatR;

namespace CargoHub.Application.Billing.AdminPlans;

public sealed record DeleteAdminSubscriptionPlanCommand(Guid PlanId) : IRequest<AdminPlanMutationResult>;

public sealed class DeleteAdminSubscriptionPlanCommandHandler
    : IRequestHandler<DeleteAdminSubscriptionPlanCommand, AdminPlanMutationResult>
{
    private readonly ISubscriptionPlanAdminRepository _repo;

    public DeleteAdminSubscriptionPlanCommandHandler(ISubscriptionPlanAdminRepository repo) => _repo = repo;

    public Task<AdminPlanMutationResult> Handle(
        DeleteAdminSubscriptionPlanCommand request,
        CancellationToken cancellationToken) =>
        _repo.DeletePlanAsync(request.PlanId, cancellationToken);
}
