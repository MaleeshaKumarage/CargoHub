using Microsoft.Extensions.Options;

namespace CargoHub.Infrastructure.Couriers;

/// <summary>
/// Runs after all configuration sources. If <see cref="SmtpOptions.Host"/> or <see cref="SmtpOptions.FromAddress"/>
/// are still blank (common when <c>Smtp__Host=</c> in <c>.env</c> overrides appsettings with an empty string),
/// applies the same placeholders as <c>appsettings.json</c> so the app fails at SMTP connect/auth instead of
/// "Host is not configured".
/// </summary>
public sealed class SmtpOptionsPostConfigureFillEmpty : IPostConfigureOptions<SmtpOptions>
{
    public void PostConfigure(string? name, SmtpOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host))
            options.Host = "smtp.example.com";

        if (string.IsNullOrWhiteSpace(options.FromAddress))
            options.FromAddress = "noreply@example.com";
    }
}
