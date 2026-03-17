using HiavaNet.Application.Bookings.Dtos;
using MediatR;

namespace HiavaNet.Application.Bookings.Commands;

public sealed record ConfirmDraftCommand(Guid DraftId, string CustomerId) : IRequest<BookingDetailDto?>;
