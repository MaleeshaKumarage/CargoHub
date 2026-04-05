using CargoHub.Application.Billing.Admin;
using MediatR;

namespace CargoHub.Application.Billing.AdminInvoicing;

public sealed record GetBillingInvoicePdfModelQuery(
    Guid PeriodId,
    DateTime? InvoiceRangeStartUtc = null,
    DateTime? InvoiceRangeEndExclusiveUtc = null) : IRequest<BillingInvoicePdfModel?>;
