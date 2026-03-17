using HiavaNet.Application.Bookings.Dtos;
using MediatR;

namespace HiavaNet.Application.Bookings.Queries;

public sealed class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardBookingStatsDto>
{
    private readonly IBookingRepository _repository;

    public GetDashboardStatsQueryHandler(IBookingRepository repository)
    {
        _repository = repository;
    }

    public Task<DashboardBookingStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetDashboardStatsAsync(request.CustomerId, cancellationToken);
    }
}
