using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Queries;

public sealed class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardBookingStatsDto>
{
    private readonly IBookingRepository _repository;

    public GetDashboardStatsQueryHandler(IBookingRepository repository)
    {
        _repository = repository;
    }

    public Task<DashboardBookingStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetDashboardStatsAsync(
            request.CustomerId,
            request.Scope,
            request.HeatmapYear,
            request.HeatmapMonth,
            cancellationToken);
    }
}
