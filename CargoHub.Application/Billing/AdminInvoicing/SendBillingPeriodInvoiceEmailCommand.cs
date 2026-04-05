using MediatR;

namespace CargoHub.Application.Billing.AdminInvoicing;

public sealed record SendBillingPeriodInvoiceEmailCommand(
    Guid PeriodId,
    string RecipientAdminUserId,
    string SuperAdminUserId) : IRequest<SendInvoiceEmailResult>;

public sealed class SendInvoiceEmailResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }

    public static SendInvoiceEmailResult Ok() => new() { Success = true };

    public static SendInvoiceEmailResult Fail(string code, string msg) =>
        new() { Success = false, ErrorCode = code, Message = msg };
}
