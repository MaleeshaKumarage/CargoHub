using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Commands;

public sealed record ConfirmDraftCommand(Guid DraftId, string CustomerId) : IRequest<BookingDetailDto?>;
