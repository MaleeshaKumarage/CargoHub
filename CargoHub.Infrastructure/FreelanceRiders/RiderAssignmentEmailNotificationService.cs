using CargoHub.Application.Couriers;
using CargoHub.Application.FreelanceRiders;
using CargoHub.Infrastructure.Options;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CargoHub.Infrastructure.FreelanceRiders;

public sealed class RiderAssignmentEmailNotificationService : IRiderAssignmentNotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<PortalPublicOptions> _portal;
    private readonly IConfiguration _configuration;
    private readonly IOptions<RiderAssignmentOptions> _riderOpt;
    private readonly ILogger<RiderAssignmentEmailNotificationService> _logger;

    public RiderAssignmentEmailNotificationService(
        ApplicationDbContext db,
        IEmailSender emailSender,
        IOptions<PortalPublicOptions> portal,
        IConfiguration configuration,
        IOptions<RiderAssignmentOptions> riderOpt,
        ILogger<RiderAssignmentEmailNotificationService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _portal = portal;
        _configuration = configuration;
        _riderOpt = riderOpt;
        _logger = logger;
    }

    public async Task NotifyAssignmentAsync(Guid bookingId, Guid freelanceRiderId, CancellationToken cancellationToken = default)
    {
        var rider = await _db.FreelanceRiders.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == freelanceRiderId, cancellationToken);
        if (rider == null || string.IsNullOrWhiteSpace(rider.Email))
        {
            _logger.LogWarning("Rider {RiderId} missing for assignment notification.", freelanceRiderId);
            return;
        }

        var baseUrl = PortalPublicBaseUrlResolver.Resolve(_portal.Value, _configuration).TrimEnd('/');
        var minutes = Math.Max(1, _riderOpt.Value.AcceptanceWindowMinutes);
        var path = $"/en/rider/deliveries/{bookingId:N}";
        var link = $"{baseUrl}{path}";

        var subject = "New delivery request — please accept";
        var body =
            $"<p>You have been assigned a delivery. Please open the portal and accept within <strong>{minutes} minutes</strong>.</p>" +
            $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">Open delivery</a></p>" +
            "<p>If you do not accept in time, the shipment will proceed with the customer’s selected courier.</p>";

        try
        {
            await _emailSender.SendAsync(rider.Email.Trim(), subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send rider assignment email for booking {BookingId}.", bookingId);
        }
    }
}
