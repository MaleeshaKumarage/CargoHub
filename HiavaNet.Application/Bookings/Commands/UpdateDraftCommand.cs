using HiavaNet.Application.Bookings.Dtos;
using MediatR;

namespace HiavaNet.Application.Bookings.Commands;

public sealed record UpdateDraftCommand(Guid DraftId, string CustomerId, UpdateDraftRequest Request) : IRequest<BookingDetailDto?>;
