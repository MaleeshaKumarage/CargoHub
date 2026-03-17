using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Queries;

/// <param name="CustomerId">When null, returns stats for all users (Super Admin). Otherwise stats for that customer only.</param>
public sealed record GetDashboardStatsQuery(string? CustomerId) : IRequest<DashboardBookingStatsDto>;
