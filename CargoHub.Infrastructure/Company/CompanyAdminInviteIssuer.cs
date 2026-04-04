using CargoHub.Application.Company;
using CargoHub.Application.Couriers;
using CargoHub.Domain.Companies;
using CargoHub.Infrastructure.Options;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CargoHub.Infrastructure.Company;

/// <summary>
/// Persists a company admin invite and sends the link by email when SMTP is configured.
/// </summary>
public sealed class CompanyAdminInviteIssuer : ICompanyAdminInviteIssuer
{
    private readonly ICompanyAdminInviteRepository _invites;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<PortalPublicOptions> _portal;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CompanyAdminInviteIssuer> _logger;

    public CompanyAdminInviteIssuer(
        ICompanyAdminInviteRepository invites,
        IEmailSender emailSender,
        IOptions<PortalPublicOptions> portal,
        ApplicationDbContext db,
        ILogger<CompanyAdminInviteIssuer> logger)
    {
        _invites = invites;
        _emailSender = emailSender;
        _portal = portal;
        _db = db;
        _logger = logger;
    }

    public Task TryIssueInitialAdminInviteAsync(
        Guid companyId,
        string businessId,
        string? explicitSuperAdminEmail,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string>? list = string.IsNullOrWhiteSpace(explicitSuperAdminEmail)
            ? null
            : new[] { explicitSuperAdminEmail.Trim() };
        return TryIssueInitialAdminInvitesAsync(companyId, businessId, list, cancellationToken);
    }

    public async Task TryIssueInitialAdminInvitesAsync(
        Guid companyId,
        string businessId,
        IReadOnlyList<string>? explicitEmails,
        CancellationToken cancellationToken = default)
    {
        var domain = _portal.Value.CompanyAdminFallbackEmailDomain;
        var emails = CompanyAdminInviteEmailsHelper.NormalizeList(explicitEmails);
        if (emails.Count == 0)
        {
            var fallback = CompanyAdminInviteAddress.BuildFallbackEmail(businessId, domain);
            await TryIssueInviteAsync(companyId, fallback, cancellationToken);
            return;
        }

        foreach (var email in emails)
            await TryIssueInviteAsync(companyId, email, cancellationToken);
    }

    /// <returns>Raw token (for tests); do not log in production.</returns>
    public async Task<string?> TryIssueInviteAsync(
        Guid companyId,
        string inviteEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inviteEmail))
            return null;

        var email = inviteEmail.Trim();
        var normalized = email.ToUpperInvariant();

        await _invites.RevokePendingForCompanyAndEmailAsync(companyId, normalized, cancellationToken);

        var raw = CompanyInviteTokenHelper.GenerateRawToken();
        var hash = CompanyInviteTokenHelper.HashRawToken(raw);

        var invite = new CompanyAdminInvite
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Email = email,
            NormalizedEmail = normalized,
            TokenHash = hash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _invites.AddAsync(invite, cancellationToken);

        var baseUrl = _portal.Value.PublicBaseUrl.TrimEnd('/');
        var link = $"{baseUrl}/en/accept-invite?token={Uri.EscapeDataString(raw)}";

        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        var companyLabel = company?.Name ?? company?.BusinessId ?? companyId.ToString();

        var subject = $"Company admin invitation — {companyLabel}";
        var body =
            $"<p>You have been invited as an administrator for <strong>{System.Net.WebUtility.HtmlEncode(companyLabel)}</strong>.</p>" +
            $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">Accept invitation and set your password</a></p>" +
            "<p>If the link expires, ask a Super Admin to resend the invite.</p>";

        try
        {
            await _emailSender.SendAsync(email, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send company admin invite email to {Email}; invite row exists.", email);
        }

        return raw;
    }
}
