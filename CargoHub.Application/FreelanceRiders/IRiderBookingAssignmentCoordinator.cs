using CargoHub.Domain.Bookings;

namespace CargoHub.Application.FreelanceRiders;

/// <summary>Validates rider selection and applies assignment fields + notifications for completed bookings.</summary>
public interface IRiderBookingAssignmentCoordinator
{
    /// <summary>
    /// For a non-draft booking: validate rider, set deadline, send notification. Pass <paramref name="freelanceRiderId"/> null to clear.
    /// </summary>
    Task<RiderAssignmentApplyResult> ApplyForCompletedBookingAsync(
        Booking booking,
        Guid? freelanceRiderId,
        CancellationToken cancellationToken = default);
}

public sealed class RiderAssignmentApplyResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }

    public static RiderAssignmentApplyResult Ok() => new() { Success = true };

    public static RiderAssignmentApplyResult Fail(string code, string message) =>
        new() { Success = false, ErrorCode = code, Message = message };
}
