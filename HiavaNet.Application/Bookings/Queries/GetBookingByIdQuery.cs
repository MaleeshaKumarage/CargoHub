using HiavaNet.Application.Bookings.Dtos;
using MediatR;

namespace HiavaNet.Application.Bookings.Queries;

/// <param name="CustomerId">When null (SuperAdmin), allows viewing any booking.</param>
public sealed record GetBookingByIdQuery(Guid Id, string? CustomerId) : IRequest<BookingDetailDto?>;
