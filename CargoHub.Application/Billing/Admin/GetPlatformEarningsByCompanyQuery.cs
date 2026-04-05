using MediatR;

namespace CargoHub.Application.Billing.Admin;

public sealed record GetPlatformEarningsByCompanyQuery(int YearUtc, int MonthUtc) : IRequest<IReadOnlyList<PlatformEarningsCompanyDto>>;

public sealed class GetPlatformEarningsByCompanyQueryHandler : IRequestHandler<GetPlatformEarningsByCompanyQuery, IReadOnlyList<PlatformEarningsCompanyDto>>
{
    private readonly IAdminPlatformEarningsReader _reader;

    public GetPlatformEarningsByCompanyQueryHandler(IAdminPlatformEarningsReader reader) => _reader = reader;

    public Task<IReadOnlyList<PlatformEarningsCompanyDto>> Handle(GetPlatformEarningsByCompanyQuery request, CancellationToken cancellationToken) =>
        _reader.GetByCompanyForMonthAsync(request.YearUtc, request.MonthUtc, cancellationToken);
}
