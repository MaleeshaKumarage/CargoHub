using Microsoft.Extensions.Configuration;

namespace CargoHub.Infrastructure.Options;

/// <summary>
/// Resolves the public portal origin for absolute links in emails (invites, etc.).
/// Prefer <see cref="PortalPublicOptions.PublicBaseUrl"/>; fall back to CORS portal settings.
/// </summary>
public static class PortalPublicBaseUrlResolver
{
    public static string Resolve(PortalPublicOptions portal, IConfiguration configuration)
    {
        var fromPortal = portal.PublicBaseUrl?.Trim();
        if (!string.IsNullOrEmpty(fromPortal))
            return fromPortal.TrimEnd('/');

        var corsOrigin = configuration["Cors:PortalOrigin"]?.Trim();
        if (!string.IsNullOrEmpty(corsOrigin))
            return corsOrigin.TrimEnd('/');

        var origins = configuration.GetSection("Cors:PortalOrigins").Get<string[]>();
        var first = origins?.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))?.Trim();
        if (!string.IsNullOrEmpty(first))
            return first.TrimEnd('/');

        return "http://localhost:3000";
    }
}
