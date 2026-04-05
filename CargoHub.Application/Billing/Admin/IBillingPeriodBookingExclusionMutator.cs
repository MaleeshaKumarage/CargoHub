namespace CargoHub.Application.Billing.Admin;

public sealed class BillingPeriodBookingExclusionResult
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public string? Message { get; init; }

    public static BillingPeriodBookingExclusionResult Ok() => new() { Success = true };

    public static BillingPeriodBookingExclusionResult Fail(string code, string message) =>
        new() { Success = false, ErrorCode = code, Message = message };
}

public interface IBillingPeriodBookingExclusionMutator
{
    Task<BillingPeriodBookingExclusionResult> SetExcludedAsync(
        Guid companyBillingPeriodId,
        Guid bookingId,
        bool excluded,
        string? superAdminUserId,
        CancellationToken cancellationToken = default);
}
