using MediatR;

namespace CargoHub.Application.Billing.AdminPlans;

public sealed record CreateAdminSubscriptionPlanCommand(
    string Name,
    string Kind,
    string ChargeTimeAnchor,
    int? TrialBookingAllowance,
    string Currency,
    bool IsActive) : IRequest<CreatePlanResult>;

public sealed class CreatePlanResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }
    public Guid? PlanId { get; init; }

    public static CreatePlanResult Created(Guid id) => new() { Success = true, PlanId = id };

    public static CreatePlanResult Fail(string code, string msg) =>
        new() { Success = false, ErrorCode = code, Message = msg };
}

public sealed class CreateAdminSubscriptionPlanCommandHandler : IRequestHandler<CreateAdminSubscriptionPlanCommand, CreatePlanResult>
{
    private readonly ISubscriptionPlanAdminRepository _repo;

    public CreateAdminSubscriptionPlanCommandHandler(ISubscriptionPlanAdminRepository repo) => _repo = repo;

    public async Task<CreatePlanResult> Handle(CreateAdminSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
            return CreatePlanResult.Fail("NameRequired", "Plan name is required.");

        if (!Enum.TryParse<Domain.Billing.SubscriptionPlanKind>(request.Kind, true, out var kind))
            return CreatePlanResult.Fail("InvalidKind", "Invalid subscription plan kind.");

        if (!Enum.TryParse<Domain.Billing.ChargeTimeAnchor>(request.ChargeTimeAnchor, true, out var anchor))
            return CreatePlanResult.Fail("InvalidAnchor", "Invalid charge time anchor.");

        if (kind == Domain.Billing.SubscriptionPlanKind.Trial && (!request.TrialBookingAllowance.HasValue || request.TrialBookingAllowance < 1))
            return CreatePlanResult.Fail("TrialAllowanceRequired", "Trial plans require a positive trial booking allowance.");

        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency.Trim().ToUpperInvariant();
        if (currency.Length != 3)
            return CreatePlanResult.Fail("InvalidCurrency", "Currency must be a 3-letter ISO code.");

        try
        {
            var id = await _repo.CreatePlanAsync(
                name,
                kind.ToString(),
                anchor.ToString(),
                kind == Domain.Billing.SubscriptionPlanKind.Trial ? request.TrialBookingAllowance : null,
                currency,
                request.IsActive,
                cancellationToken);
            return CreatePlanResult.Created(id);
        }
        catch (Exception ex)
        {
            return CreatePlanResult.Fail("CreateFailed", ex.Message);
        }
    }
}
