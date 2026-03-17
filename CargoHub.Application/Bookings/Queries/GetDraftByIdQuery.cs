using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Queries;

/// <param name="CustomerId">When null (SuperAdmin), allows viewing any draft.</param>
public sealed record GetDraftByIdQuery(Guid Id, string? CustomerId) : IRequest<BookingDetailDto?>;
