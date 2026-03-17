using CargoHub.Application.Bookings.Dtos;
using CargoHub.Application.Bookings.Queries;
using MediatR;

namespace CargoHub.Application.Bookings.Commands;

public sealed class ConfirmDraftCommandHandler : IRequestHandler<ConfirmDraftCommand, BookingDetailDto?>
{
    private readonly IBookingRepository _repository;

    public ConfirmDraftCommandHandler(IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<BookingDetailDto?> Handle(ConfirmDraftCommand request, CancellationToken cancellationToken)
    {
        var confirmed = await _repository.ConfirmDraftAsync(request.DraftId, request.CustomerId, cancellationToken);
        if (!confirmed)
            return null;
        var booking = await _repository.GetByIdAsync(request.DraftId, cancellationToken);
        return booking != null ? GetBookingByIdQueryHandler.MapToDetail(booking) : null;
    }
}
