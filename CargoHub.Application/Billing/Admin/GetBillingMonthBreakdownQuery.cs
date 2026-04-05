using MediatR;

namespace CargoHub.Application.Billing.Admin;

public sealed record GetBillingMonthBreakdownQuery(Guid CompanyId, int YearUtc, int MonthUtc)
    : IRequest<BillingMonthBreakdownDto?>;

public sealed class GetBillingMonthBreakdownQueryHandler
    : IRequestHandler<GetBillingMonthBreakdownQuery, BillingMonthBreakdownDto?>
{
    private readonly IBillingMonthBreakdownReader _reader;

    public GetBillingMonthBreakdownQueryHandler(IBillingMonthBreakdownReader reader) => _reader = reader;

    public Task<BillingMonthBreakdownDto?> Handle(
        GetBillingMonthBreakdownQuery request,
        CancellationToken cancellationToken) =>
        _reader.GetBreakdownAsync(request.CompanyId, request.YearUtc, request.MonthUtc, cancellationToken);
}
