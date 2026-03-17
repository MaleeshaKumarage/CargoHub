using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Queries;

public sealed class GetDraftByIdQueryHandler : IRequestHandler<GetDraftByIdQuery, BookingDetailDto?>
{
    private readonly IBookingRepository _repository;

    public GetDraftByIdQueryHandler(IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<BookingDetailDto?> Handle(GetDraftByIdQuery request, CancellationToken cancellationToken)
    {
        var b = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (b == null || !b.IsDraft)
            return null;
        if (!string.IsNullOrEmpty(request.CustomerId) && b.CustomerId != request.CustomerId)
            return null;
        var dto = GetBookingByIdQueryHandler.MapToDetail(b);
        try
        {
            dto.StatusHistory = await _repository.GetStatusHistoryAsync(request.Id, cancellationToken);
        }
        catch
        {
            dto.StatusHistory = new List<BookingStatusEventDto>();
        }
        return dto;
    }
}
