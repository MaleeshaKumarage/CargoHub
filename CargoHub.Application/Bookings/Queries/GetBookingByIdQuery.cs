using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Queries;

/// <param name="CustomerId">When null (SuperAdmin), allows viewing any booking.</param>
public sealed record GetBookingByIdQuery(Guid Id, string? CustomerId) : IRequest<BookingDetailDto?>;
