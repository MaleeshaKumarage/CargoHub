using MediatR;

namespace CargoHub.Application.Billing.AdminInvoicing;

public sealed class SendBillingPeriodInvoiceEmailCommandHandler
    : IRequestHandler<SendBillingPeriodInvoiceEmailCommand, SendInvoiceEmailResult>
{
    private readonly IAdminBillingInvoiceOperations _operations;

    public SendBillingPeriodInvoiceEmailCommandHandler(IAdminBillingInvoiceOperations operations) =>
        _operations = operations;

    public Task<SendInvoiceEmailResult> Handle(
        SendBillingPeriodInvoiceEmailCommand request,
        CancellationToken cancellationToken) =>
        _operations.SendInvoiceEmailAsync(
            request.PeriodId,
            request.RecipientAdminUserId,
            request.SuperAdminUserId,
            cancellationToken,
            request.InvoiceRangeStartUtc,
            request.InvoiceRangeEndExclusiveUtc);
}
