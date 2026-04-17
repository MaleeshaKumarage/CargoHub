namespace CargoHub.Application.FreelanceRiders;

/// <summary>Notify rider when assigned to a booking (email v1; extend for SMS/in-app).</summary>
public interface IRiderAssignmentNotificationService
{
    Task NotifyAssignmentAsync(Guid bookingId, Guid freelanceRiderId, CancellationToken cancellationToken = default);
}
