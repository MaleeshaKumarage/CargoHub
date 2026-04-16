using CargoHub.Application.FreelanceRiders;
using CargoHub.Domain.Bookings;

namespace CargoHub.Tests.TestSupport;

/// <summary>Skips notification + DB rider validation; sets assignment fields for tests.</summary>
public sealed class StubRiderBookingAssignmentCoordinator : IRiderBookingAssignmentCoordinator
{
    public static readonly StubRiderBookingAssignmentCoordinator Instance = new();

    public Task<RiderAssignmentApplyResult> ApplyForCompletedBookingAsync(
        Booking booking,
        Guid? freelanceRiderId,
        CancellationToken cancellationToken = default)
    {
        if (!freelanceRiderId.HasValue)
        {
            booking.FreelanceRiderId = null;
            booking.FreelanceRiderAssignmentDeadlineUtc = null;
            booking.FreelanceRiderAcceptedAtUtc = null;
            booking.FreelanceRiderAssignmentLapsed = false;
            return Task.FromResult(RiderAssignmentApplyResult.Ok());
        }

        booking.FreelanceRiderId = freelanceRiderId;
        booking.FreelanceRiderAssignmentDeadlineUtc = DateTime.UtcNow.AddMinutes(10);
        booking.FreelanceRiderAcceptedAtUtc = null;
        booking.FreelanceRiderAssignmentLapsed = false;
        return Task.FromResult(RiderAssignmentApplyResult.Ok());
    }
}
