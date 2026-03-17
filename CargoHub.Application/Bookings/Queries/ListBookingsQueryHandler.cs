using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Queries;

public sealed class ListBookingsQueryHandler : IRequestHandler<ListBookingsQuery, List<BookingListDto>>
{
    private readonly IBookingRepository _repository;

    public ListBookingsQueryHandler(IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<BookingListDto>> Handle(ListBookingsQuery request, CancellationToken cancellationToken)
    {
        var list = string.IsNullOrEmpty(request.CustomerId)
            ? await _repository.ListAllAsync(request.Skip, request.Take, cancellationToken)
            : await _repository.ListByCustomerIdAsync(request.CustomerId, request.Skip, request.Take, cancellationToken);
        var ids = list.Select(b => b.Id).ToList();
        var statusByBooking = await _repository.GetStatusHistoryForBookingIdsAsync(ids, cancellationToken);
        return list.Select(b => new BookingListDto
        {
            Id = b.Id,
            ShipmentNumber = b.ShipmentNumber,
            WaybillNumber = b.WaybillNumber,
            CustomerName = b.CustomerName,
            CreatedAtUtc = b.CreatedAtUtc,
            Enabled = b.Enabled,
            IsFavourite = b.IsFavourite,
            IsDraft = b.IsDraft,
            StatusHistory = statusByBooking.TryGetValue(b.Id, out var h) ? h : new List<BookingStatusEventDto>()
        }).ToList();
    }
}
