using MediatR;

namespace CargoHub.Application.Billing.AdminInvoicing;

public sealed record UpdateBillingLineExcludedCommand(
    Guid LineId,
    bool ExcludedFromInvoice,
    string SuperAdminUserId) : IRequest<UpdateLineExcludedResult>;

public sealed class UpdateLineExcludedResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }

    public static UpdateLineExcludedResult Ok() => new() { Success = true };

    public static UpdateLineExcludedResult Fail(string code, string msg) =>
        new() { Success = false, ErrorCode = code, Message = msg };
}
