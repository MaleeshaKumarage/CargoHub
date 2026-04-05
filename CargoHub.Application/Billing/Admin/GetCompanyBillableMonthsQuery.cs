using MediatR;

namespace CargoHub.Application.Billing.Admin;

public sealed record GetCompanyBillableMonthsQuery(Guid CompanyId) : IRequest<IReadOnlyList<BillableMonthSummaryDto>>;

public sealed class GetCompanyBillableMonthsQueryHandler
    : IRequestHandler<GetCompanyBillableMonthsQuery, IReadOnlyList<BillableMonthSummaryDto>>
{
    private readonly IBillingMonthBreakdownReader _reader;

    public GetCompanyBillableMonthsQueryHandler(IBillingMonthBreakdownReader reader) => _reader = reader;

    public Task<IReadOnlyList<BillableMonthSummaryDto>> Handle(
        GetCompanyBillableMonthsQuery request,
        CancellationToken cancellationToken) =>
        _reader.GetBillableMonthsAsync(request.CompanyId, cancellationToken);
}
