namespace CargoHub.Infrastructure.Options;

/// <summary>
/// Public portal base URL and invite email defaults (appsettings section <c>Portal</c>).
/// </summary>
public sealed class PortalPublicOptions
{
    public const string SectionName = "Portal";

    /// <summary>Origin of the Next.js portal (no trailing slash), e.g. https://app.example.com. When empty, CORS portal origin is used.</summary>
    public string PublicBaseUrl { get; set; } = "";

    /// <summary>
    /// Absolute URL to the product tour (e.g. https://app.example.com/en/tour). When empty, invite emails use
    /// <c>{resolved PublicBaseUrl}/en/tour</c>. Set this when the tour is hosted on a different origin (e.g. a demo tunnel).
    /// </summary>
    public string TourUrl { get; set; } = "";

    /// <summary>Domain for fallback admin invite when Super Admin does not set an email.</summary>
    public string CompanyAdminFallbackEmailDomain { get; set; } = "example.com";

    /// <summary>Optional display name appended to admin invite emails as a contact line (with <see cref="AdminInviteContactEmail"/>).</summary>
    public string AdminInviteContactName { get; set; } = "";

    /// <summary>Optional email for the admin invite contact line (with <see cref="AdminInviteContactName"/>).</summary>
    public string AdminInviteContactEmail { get; set; } = "";
}
