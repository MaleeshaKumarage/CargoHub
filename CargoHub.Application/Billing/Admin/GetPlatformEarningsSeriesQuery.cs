using MediatR;

namespace CargoHub.Application.Billing.Admin;

public sealed record GetPlatformEarningsSeriesQuery(PlatformEarningsSeriesRange Range)
    : IRequest<IReadOnlyList<PlatformEarningsSeriesPointDto>>;

public sealed class GetPlatformEarningsSeriesQueryHandler : IRequestHandler<GetPlatformEarningsSeriesQuery, IReadOnlyList<PlatformEarningsSeriesPointDto>>
{
    private readonly IAdminPlatformEarningsReader _reader;

    public GetPlatformEarningsSeriesQueryHandler(IAdminPlatformEarningsReader reader) => _reader = reader;

    public Task<IReadOnlyList<PlatformEarningsSeriesPointDto>> Handle(
        GetPlatformEarningsSeriesQuery request,
        CancellationToken cancellationToken) =>
        _reader.GetSeriesAsync(request.Range, cancellationToken);
}
