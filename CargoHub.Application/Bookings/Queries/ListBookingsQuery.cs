using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Queries;

/// <param name="CustomerId">When null (SuperAdmin), returns all companies' bookings.</param>
public sealed record ListBookingsQuery(string? CustomerId, int Skip = 0, int Take = 100) : IRequest<List<BookingListDto>>;
