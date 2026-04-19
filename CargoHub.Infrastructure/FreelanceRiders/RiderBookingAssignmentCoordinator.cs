using CargoHub.Application.FreelanceRiders;
using CargoHub.Domain.Bookings;
using CargoHub.Domain.FreelanceRiders;
using Microsoft.Extensions.Options;

namespace CargoHub.Infrastructure.FreelanceRiders;

/// <inheritdoc />
public sealed class RiderBookingAssignmentCoordinator : IRiderBookingAssignmentCoordinator
{
    private readonly IFreelanceRiderRepository _riders;
    private readonly IRiderAssignmentNotificationService _notify;
    private readonly RiderAssignmentOptions _opt;

    public RiderBookingAssignmentCoordinator(
        IFreelanceRiderRepository riders,
        IRiderAssignmentNotificationService notify,
        IOptions<RiderAssignmentOptions> opt)
    {
        _riders = riders;
        _notify = notify;
        _opt = opt.Value;
    }

    public async Task<RiderAssignmentApplyResult> ApplyForCompletedBookingAsync(
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
            return RiderAssignmentApplyResult.Ok();
        }

        var rider = await _riders.GetByIdAsync(freelanceRiderId.Value, true, cancellationToken);
        if (rider == null || rider.Status != FreelanceRiderStatus.Active)
            return RiderAssignmentApplyResult.Fail("RiderNotFound", "Selected rider is not available.");

        if (rider.CompanyId.HasValue &&
            (!booking.CompanyId.HasValue || rider.CompanyId.Value != booking.CompanyId.Value))
            return RiderAssignmentApplyResult.Fail("RiderNotVisibleForCompany", "That rider is not available for your company.");

        var ship = RiderPostalNormalizer.Normalize(booking.Shipper.PostalCode);
        var recv = RiderPostalNormalizer.Normalize(booking.Receiver.PostalCode);
        if (string.IsNullOrEmpty(ship) || string.IsNullOrEmpty(recv))
            return RiderAssignmentApplyResult.Fail("PostalRequired", "Shipper and receiver postal codes are required to assign a rider.");

        var areaSet = rider.ServiceAreas.Select(a => a.PostalCodeNormalized).ToHashSet();
        if (!areaSet.Contains(ship) || !areaSet.Contains(recv))
            return RiderAssignmentApplyResult.Fail("RiderPostalMismatch", "The rider does not cover this route.");

        booking.FreelanceRiderId = rider.Id;
        booking.FreelanceRiderAssignmentDeadlineUtc =
            DateTime.UtcNow.AddMinutes(Math.Max(1, _opt.AcceptanceWindowMinutes));
        booking.FreelanceRiderAcceptedAtUtc = null;
        booking.FreelanceRiderAssignmentLapsed = false;

        await _notify.NotifyAssignmentAsync(booking.Id, rider.Id, cancellationToken);
        return RiderAssignmentApplyResult.Ok();
    }
}
