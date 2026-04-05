using MediatR;

namespace CargoHub.Application.Billing.AdminInvoicing;

public sealed class UpdateBillingLineExcludedCommandHandler
    : IRequestHandler<UpdateBillingLineExcludedCommand, UpdateLineExcludedResult>
{
    private readonly IAdminBillingInvoiceOperations _operations;

    public UpdateBillingLineExcludedCommandHandler(IAdminBillingInvoiceOperations operations) =>
        _operations = operations;

    public Task<UpdateLineExcludedResult> Handle(
        UpdateBillingLineExcludedCommand request,
        CancellationToken cancellationToken) =>
        _operations.UpdateLineExcludedAsync(
            request.LineId,
            request.ExcludedFromInvoice,
            request.SuperAdminUserId,
            cancellationToken);
}
