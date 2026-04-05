using MediatR;

namespace CargoHub.Application.Billing.Admin;

public sealed record GetPlatformEarningsMonthlyQuery(int Months) : IRequest<IReadOnlyList<PlatformEarningsMonthDto>>;

public sealed class GetPlatformEarningsMonthlyQueryHandler : IRequestHandler<GetPlatformEarningsMonthlyQuery, IReadOnlyList<PlatformEarningsMonthDto>>
{
    private readonly IAdminPlatformEarningsReader _reader;

    public GetPlatformEarningsMonthlyQueryHandler(IAdminPlatformEarningsReader reader) => _reader = reader;

    public Task<IReadOnlyList<PlatformEarningsMonthDto>> Handle(GetPlatformEarningsMonthlyQuery request, CancellationToken cancellationToken) =>
        _reader.GetMonthlyTotalsAsync(request.Months, cancellationToken);
}
