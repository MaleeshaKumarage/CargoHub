namespace CargoHub.Api.Options;

/// <summary>
/// Deployment-level branding: app name, logo, theme colors, and waybill footer.
/// Configure via appsettings "Branding" section or environment variables.
/// </summary>
public class BrandingOptions
{
    public const string SectionName = "Branding";

    /// <summary>Display name for the portal (e.g. "Acme Portal").</summary>
    public string? AppName { get; set; }

    /// <summary>URL or path to the company logo (e.g. "/assets/logo.svg").</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Primary theme color (hex, e.g. "#1a1a2e").</summary>
    public string? PrimaryColor { get; set; }

    /// <summary>Secondary/accent theme color (hex, e.g. "#16213e").</summary>
    public string? SecondaryColor { get; set; }

    /// <summary>Footer text for waybill PDF (e.g. "Acme — Waybill").</summary>
    public string? WaybillFooterText { get; set; }
}
