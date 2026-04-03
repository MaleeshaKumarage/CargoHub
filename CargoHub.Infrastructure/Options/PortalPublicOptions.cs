namespace CargoHub.Infrastructure.Options;

/// <summary>
/// Public portal base URL and invite email defaults (appsettings section <c>Portal</c>).
/// </summary>
public sealed class PortalPublicOptions
{
    public const string SectionName = "Portal";

    /// <summary>Origin of the Next.js portal (no trailing slash), e.g. https://app.example.com</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:3000";

    /// <summary>Domain for fallback admin invite when Super Admin does not set an email.</summary>
    public string CompanyAdminFallbackEmailDomain { get; set; } = "example.com";
}
