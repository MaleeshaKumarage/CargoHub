using MediatR;

namespace CargoHub.Application.Billing.AdminPlans;

public sealed record AddAdminPricingPeriodCommand(
    Guid PlanId,
    DateTime EffectiveFromUtc,
    decimal? ChargePerBooking,
    decimal? MonthlyFee,
    int? IncludedBookingsPerMonth,
    decimal? OverageChargePerBooking) : IRequest<AdminPlanMutationResult>;

public sealed class AddAdminPricingPeriodCommandHandler
    : IRequestHandler<AddAdminPricingPeriodCommand, AdminPlanMutationResult>
{
    private readonly ISubscriptionPlanAdminRepository _repo;

    public AddAdminPricingPeriodCommandHandler(ISubscriptionPlanAdminRepository repo) => _repo = repo;

    public Task<AdminPlanMutationResult> Handle(
        AddAdminPricingPeriodCommand request,
        CancellationToken cancellationToken) =>
        _repo.AddPricingPeriodAsync(
            request.PlanId,
            request.EffectiveFromUtc,
            request.ChargePerBooking,
            request.MonthlyFee,
            request.IncludedBookingsPerMonth,
            request.OverageChargePerBooking,
            cancellationToken);
}

public sealed record UpdateAdminPricingPeriodCommand(
    Guid PeriodId,
    DateTime EffectiveFromUtc,
    decimal? ChargePerBooking,
    decimal? MonthlyFee,
    int? IncludedBookingsPerMonth,
    decimal? OverageChargePerBooking) : IRequest<AdminPlanMutationResult>;

public sealed class UpdateAdminPricingPeriodCommandHandler
    : IRequestHandler<UpdateAdminPricingPeriodCommand, AdminPlanMutationResult>
{
    private readonly ISubscriptionPlanAdminRepository _repo;

    public UpdateAdminPricingPeriodCommandHandler(ISubscriptionPlanAdminRepository repo) => _repo = repo;

    public Task<AdminPlanMutationResult> Handle(
        UpdateAdminPricingPeriodCommand request,
        CancellationToken cancellationToken) =>
        _repo.UpdatePricingPeriodAsync(
            request.PeriodId,
            request.EffectiveFromUtc,
            request.ChargePerBooking,
            request.MonthlyFee,
            request.IncludedBookingsPerMonth,
            request.OverageChargePerBooking,
            cancellationToken);
}

public sealed record DeleteAdminPricingPeriodCommand(Guid PeriodId) : IRequest<AdminPlanMutationResult>;

public sealed class DeleteAdminPricingPeriodCommandHandler
    : IRequestHandler<DeleteAdminPricingPeriodCommand, AdminPlanMutationResult>
{
    private readonly ISubscriptionPlanAdminRepository _repo;

    public DeleteAdminPricingPeriodCommandHandler(ISubscriptionPlanAdminRepository repo) => _repo = repo;

    public Task<AdminPlanMutationResult> Handle(
        DeleteAdminPricingPeriodCommand request,
        CancellationToken cancellationToken) =>
        _repo.DeletePricingPeriodAsync(request.PeriodId, cancellationToken);
}
