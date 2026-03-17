using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Dtos;
using MediatR;


namespace CargoHub.Application.Bookings.Queries;

/// <param name="CustomerId">When null (SuperAdmin), returns all companies' bookings.</param>
/// <param name="Filter">Optional filter (createdFrom, createdTo, enabled). When null, exports all.</param>
public sealed record ExportBookingsQuery(string? CustomerId, int Skip = 0, int Take = 1000, BookingListFilter? Filter = null) : IRequest<List<BookingDetailDto>>;
