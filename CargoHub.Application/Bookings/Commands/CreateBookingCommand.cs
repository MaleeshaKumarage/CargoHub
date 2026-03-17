using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Commands;

public sealed record CreateBookingCommand(string CustomerId, string? CustomerName, CreateBookingRequest Request, Guid? CompanyId = null) : IRequest<BookingDetailDto?>;
