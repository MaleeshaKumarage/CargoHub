namespace CargoHub.Application.Billing.AdminPlans;

public sealed class AdminPlanMutationResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }

    public static AdminPlanMutationResult Ok() => new() { Success = true };

    public static AdminPlanMutationResult Fail(string code, string message) =>
        new() { Success = false, ErrorCode = code, Message = message };
}
