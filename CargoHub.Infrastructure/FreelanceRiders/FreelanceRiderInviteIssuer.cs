using CargoHub.Application.FreelanceRiders;
using CargoHub.Application.Couriers;
using CargoHub.Domain.FreelanceRiders;
using CargoHub.Infrastructure.Options;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CargoHub.Infrastructure.FreelanceRiders;

public sealed class FreelanceRiderInviteIssuer
{
    private readonly IFreelanceRiderInviteRepository _invites;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<PortalPublicOptions> _portal;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<FreelanceRiderInviteIssuer> _logger;

    public FreelanceRiderInviteIssuer(
        IFreelanceRiderInviteRepository invites,
        IEmailSender emailSender,
        IOptions<PortalPublicOptions> portal,
        IConfiguration configuration,
        ApplicationDbContext db,
        ILogger<FreelanceRiderInviteIssuer> logger)
    {
        _invites = invites;
        _emailSender = emailSender;
        _portal = portal;
        _configuration = configuration;
        _db = db;
        _logger = logger;
    }

    public async Task<string?> IssueInviteAsync(Guid freelanceRiderId, CancellationToken cancellationToken = default)
    {
        var rider = await _db.FreelanceRiders.AsNoTracking().FirstOrDefaultAsync(r => r.Id == freelanceRiderId, cancellationToken);
        if (rider == null || string.IsNullOrWhiteSpace(rider.Email))
            return null;

        var email = rider.Email.Trim();
        var normalized = email.ToUpperInvariant();
        await _invites.RevokePendingForRiderAndEmailAsync(freelanceRiderId, normalized, cancellationToken);

        var raw = CompanyInviteTokenHelper.GenerateRawToken();
        var hash = CompanyInviteTokenHelper.HashRawToken(raw);

        var invite = new FreelanceRiderInvite
        {
            Id = Guid.NewGuid(),
            FreelanceRiderId = freelanceRiderId,
            Email = email,
            NormalizedEmail = normalized,
            TokenHash = hash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _invites.AddAsync(invite, cancellationToken);

        var baseUrl = PortalPublicBaseUrlResolver.Resolve(_portal.Value, _configuration).TrimEnd('/');
        var link = $"{baseUrl}/en/accept-rider-invite?token={Uri.EscapeDataString(raw)}";
        var subject = "Freelance rider invitation";
        var body =
            $"<p>You have been invited to join as a freelance rider (<strong>{System.Net.WebUtility.HtmlEncode(rider.DisplayName)}</strong>).</p>" +
            $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">Accept invitation</a> and set your password.</p>";

        try
        {
            await _emailSender.SendAsync(email, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send rider invite email for {RiderId}", freelanceRiderId);
        }

        return raw;
    }
}
