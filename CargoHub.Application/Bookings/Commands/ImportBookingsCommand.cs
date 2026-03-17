using CargoHub.Application.Bookings.Dtos;
using MediatR;

namespace CargoHub.Application.Bookings.Commands;

/// <summary>
/// Import bookings from parsed rows. Each row is either created as completed or draft based on data completeness.
/// </summary>
public sealed record ImportBookingsCommand(
    string CustomerId,
    string CustomerName,
    Guid? CompanyId,
    IReadOnlyList<ImportRowDto> Rows
) : IRequest<ImportBookingsResult>;

/// <summary>Single row from import file.</summary>
public sealed record ImportRowDto(CreateBookingRequest Request, bool IsComplete);

/// <summary>Result of bulk import.</summary>
public sealed record ImportBookingsResult(
    int CreatedCount,
    int DraftCount,
    IReadOnlyList<string> Errors
);
