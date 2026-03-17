using HiavaNet.Application.Bookings.Dtos;
using MediatR;

namespace HiavaNet.Application.Bookings.Queries;

public sealed class ListDraftsQueryHandler : IRequestHandler<ListDraftsQuery, List<BookingListDto>>
{
    private readonly IBookingRepository _repository;

    public ListDraftsQueryHandler(IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<BookingListDto>> Handle(ListDraftsQuery request, CancellationToken cancellationToken)
    {
        var list = string.IsNullOrEmpty(request.CustomerId)
            ? await _repository.ListAllDraftsAsync(request.Skip, request.Take, cancellationToken)
            : await _repository.ListDraftsByCustomerIdAsync(request.CustomerId, request.Skip, request.Take, cancellationToken);
        var ids = list.Select(b => b.Id).ToList();
        var statusByBooking = await _repository.GetStatusHistoryForBookingIdsAsync(ids, cancellationToken);
        return list.Select(b => new BookingListDto
        {
            Id = b.Id,
            ShipmentNumber = b.ShipmentNumber,
            CustomerName = b.CustomerName,
            CreatedAtUtc = b.CreatedAtUtc,
            Enabled = b.Enabled,
            IsFavourite = b.IsFavourite,
            IsDraft = true,
            StatusHistory = statusByBooking.TryGetValue(b.Id, out var h) ? h : new List<BookingStatusEventDto>()
        }).ToList();
    }
}
