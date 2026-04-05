using MediatR;

namespace CargoHub.Application.Billing.Admin;

public sealed record GetPlatformEarningsBySubscriptionQuery(int YearUtc, int MonthUtc) : IRequest<IReadOnlyList<PlatformEarningsSubscriptionDto>>;

public sealed class GetPlatformEarningsBySubscriptionQueryHandler : IRequestHandler<GetPlatformEarningsBySubscriptionQuery, IReadOnlyList<PlatformEarningsSubscriptionDto>>
{
    private readonly IAdminPlatformEarningsReader _reader;

    public GetPlatformEarningsBySubscriptionQueryHandler(IAdminPlatformEarningsReader reader) => _reader = reader;

    public Task<IReadOnlyList<PlatformEarningsSubscriptionDto>> Handle(GetPlatformEarningsBySubscriptionQuery request, CancellationToken cancellationToken) =>
        _reader.GetBySubscriptionForMonthAsync(request.YearUtc, request.MonthUtc, cancellationToken);
}
