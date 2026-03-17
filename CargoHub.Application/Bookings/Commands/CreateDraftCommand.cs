using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Commands;

public sealed record CreateDraftCommand(string CustomerId, string? CustomerName, CreateBookingRequest Request, Guid? CompanyId = null) : IRequest<BookingDetailDto?>;
