using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Queries;

/// <param name="CustomerId">When null, returns stats for all users (Super Admin). Otherwise stats for that customer only.</param>
/// <param name="Scope">all | drafts | tests</param>
/// <param name="HeatmapYear">UTC calendar year for the month heatmap series; omit with HeatmapMonth to use current month.</param>
/// <param name="HeatmapMonth">UTC calendar month 1–12.</param>
public sealed record GetDashboardStatsQuery(
    string? CustomerId,
    string? Scope = null,
    int? HeatmapYear = null,
    int? HeatmapMonth = null) : IRequest<DashboardBookingStatsDto>;
