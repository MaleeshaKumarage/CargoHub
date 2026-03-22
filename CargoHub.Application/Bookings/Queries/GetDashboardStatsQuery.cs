using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Queries;

/// <param name="CustomerId">When null, returns stats for all users (Super Admin). Otherwise stats for that customer only.</param>
/// <param name="Scope">all | drafts | tests</param>
public sealed record GetDashboardStatsQuery(string? CustomerId, string? Scope = null) : IRequest<DashboardBookingStatsDto>;
