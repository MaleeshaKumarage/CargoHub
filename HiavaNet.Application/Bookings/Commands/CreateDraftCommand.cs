using HiavaNet.Application.Bookings.Dtos;
using MediatR;

namespace HiavaNet.Application.Bookings.Commands;

public sealed record CreateDraftCommand(string CustomerId, string? CustomerName, CreateBookingRequest Request, Guid? CompanyId = null) : IRequest<BookingDetailDto?>;
