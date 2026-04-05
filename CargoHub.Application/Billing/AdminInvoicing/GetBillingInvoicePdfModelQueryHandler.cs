using CargoHub.Application.Billing.Admin;
using MediatR;

namespace CargoHub.Application.Billing.AdminInvoicing;

public sealed class GetBillingInvoicePdfModelQueryHandler : IRequestHandler<GetBillingInvoicePdfModelQuery, BillingInvoicePdfModel?>
{
    private readonly IAdminBillingReader _reader;

    public GetBillingInvoicePdfModelQueryHandler(IAdminBillingReader reader) => _reader = reader;

    public Task<BillingInvoicePdfModel?> Handle(GetBillingInvoicePdfModelQuery request, CancellationToken cancellationToken) =>
        _reader.GetInvoicePdfModelAsync(request.PeriodId, cancellationToken);
}
