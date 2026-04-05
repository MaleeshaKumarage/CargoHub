using MediatR;

namespace CargoHub.Application.Billing.Admin;

public sealed record GetBillingDateRangeBreakdownQuery(
    Guid CompanyId,
    DateTime RangeStartUtc,
    DateTime RangeEndExclusiveUtc) : IRequest<BillingMonthBreakdownDto?>;

public sealed class GetBillingDateRangeBreakdownQueryHandler
    : IRequestHandler<GetBillingDateRangeBreakdownQuery, BillingMonthBreakdownDto?>
{
    private readonly IBillingMonthBreakdownReader _reader;

    public GetBillingDateRangeBreakdownQueryHandler(IBillingMonthBreakdownReader reader) => _reader = reader;

    public Task<BillingMonthBreakdownDto?> Handle(
        GetBillingDateRangeBreakdownQuery request,
        CancellationToken cancellationToken) =>
        _reader.GetBreakdownForDateRangeAsync(
            request.CompanyId,
            request.RangeStartUtc,
            request.RangeEndExclusiveUtc,
            cancellationToken);
}
