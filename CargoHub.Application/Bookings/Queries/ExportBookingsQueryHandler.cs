using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Queries;

public sealed class ExportBookingsQueryHandler : IRequestHandler<ExportBookingsQuery, List<BookingDetailDto>>
{
    private readonly IBookingRepository _repository;

    public ExportBookingsQueryHandler(IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<BookingDetailDto>> Handle(ExportBookingsQuery request, CancellationToken cancellationToken)
    {
        var list = string.IsNullOrEmpty(request.CustomerId)
            ? await _repository.ListAllAsync(request.Skip, request.Take, cancellationToken)
            : await _repository.ListByCustomerIdAsync(request.CustomerId, request.Skip, request.Take, cancellationToken);
        var ids = list.Select(b => b.Id).ToList();
        var statusByBooking = await _repository.GetStatusHistoryForBookingIdsAsync(ids, cancellationToken);

        return list.Select(b =>
        {
            var dto = GetBookingByIdQueryHandler.MapToDetail(b);
            dto.StatusHistory = statusByBooking.TryGetValue(b.Id, out var h) ? h : new List<BookingStatusEventDto>();
            return dto;
        }).ToList();
    }
}
