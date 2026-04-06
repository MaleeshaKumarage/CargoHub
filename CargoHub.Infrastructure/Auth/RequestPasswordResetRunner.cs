using CargoHub.Application.Auth;
using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Company;
using CargoHub.Application.Couriers;
using CargoHub.Infrastructure.Identity;
using CargoHub.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CargoHub.Infrastructure.Auth;

public sealed class RequestPasswordResetRunner : IRequestPasswordResetRunner
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPasswordResetTokenStore _store;
    private readonly ICompanyRepository _companies;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<PortalPublicOptions> _portal;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RequestPasswordResetRunner> _logger;

    public RequestPasswordResetRunner(
        UserManager<ApplicationUser> userManager,
        IPasswordResetTokenStore store,
        ICompanyRepository companies,
        IEmailSender emailSender,
        IOptions<PortalPublicOptions> portal,
        IConfiguration configuration,
        ILogger<RequestPasswordResetRunner> logger)
    {
        _userManager = userManager;
        _store = store;
        _companies = companies;
        _emailSender = emailSender;
        _portal = portal;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool success, string? errorCode, string? message)> RunAsync(
        string email,
        string? env,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return (false, "NotFound", "User account not found.");

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(RoleNames.SuperAdmin))
        {
            var bid = user.BusinessId?.Trim();
            if (!string.IsNullOrEmpty(bid))
            {
                var company = await _companies.GetByBusinessIdAsync(bid, cancellationToken);
                if (company is { IsActive: false })
                    return (true, null, "Success");
            }
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        _store.Store(user.Id, token, expiresAt);

        var baseUrl = PortalPublicBaseUrlResolver.Resolve(_portal.Value, _configuration);
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning(
                "Password reset token stored but Portal.PublicBaseUrl is not configured; email not sent.");
            return (true, null, "Success");
        }

        var link = $"{baseUrl.TrimEnd('/')}/en/reset-password?token={Uri.EscapeDataString(token)}";
        const string subject = "Reset your password";
        var body =
            "<p>We received a request to reset the password for your account.</p>" +
            $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">Choose a new password</a></p>" +
            "<p>If you did not request this, you can ignore this email.</p>";

        try
        {
            var to = user.Email ?? email;
            await _emailSender.SendAsync(to, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password reset email to {Email}.", email);
        }

        return (true, null, "Success");
    }
}
