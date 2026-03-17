using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Commands;

public sealed record UpdateDraftCommand(Guid DraftId, string CustomerId, UpdateDraftRequest Request) : IRequest<BookingDetailDto?>;
