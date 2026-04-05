using MediatR;

namespace CargoHub.Application.Billing.Admin;

public sealed record ListCompanyBillingPeriodsQuery(Guid CompanyId) : IRequest<IReadOnlyList<CompanyBillingPeriodSummaryDto>>;

public sealed class ListCompanyBillingPeriodsQueryHandler : IRequestHandler<ListCompanyBillingPeriodsQuery, IReadOnlyList<CompanyBillingPeriodSummaryDto>>
{
    private readonly IAdminBillingReader _reader;

    public ListCompanyBillingPeriodsQueryHandler(IAdminBillingReader reader) => _reader = reader;

    public Task<IReadOnlyList<CompanyBillingPeriodSummaryDto>> Handle(ListCompanyBillingPeriodsQuery request, CancellationToken cancellationToken) =>
        _reader.ListBillingPeriodsForCompanyAsync(request.CompanyId, cancellationToken);
}
