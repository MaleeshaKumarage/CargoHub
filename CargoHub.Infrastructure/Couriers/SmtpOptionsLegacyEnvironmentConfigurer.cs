using Microsoft.Extensions.Options;

namespace CargoHub.Infrastructure.Couriers;

/// <summary>
/// Fills <see cref="SmtpOptions"/> from flat env vars when the <c>Smtp</c> section left them empty
/// (matches older booking-backend style: SMTP_SERVER_EMAIL, INFO_EMAIL_ADDRESS, etc.).
/// </summary>
public sealed class SmtpOptionsLegacyEnvironmentConfigurer : IConfigureOptions<SmtpOptions>
{
    public void Configure(SmtpOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host))
        {
            var h = Environment.GetEnvironmentVariable("SMTP_SERVER_EMAIL")
                ?? Environment.GetEnvironmentVariable("SMTP_HOST");
            if (!string.IsNullOrWhiteSpace(h))
                options.Host = h.Trim();
        }

        if (string.IsNullOrWhiteSpace(options.FromAddress))
        {
            var f = Environment.GetEnvironmentVariable("SMTP_FROM")
                ?? Environment.GetEnvironmentVariable("INFO_EMAIL_ADDRESS");
            if (!string.IsNullOrWhiteSpace(f))
                options.FromAddress = f.Trim();
        }

        if (string.IsNullOrWhiteSpace(options.UserName))
        {
            var u = Environment.GetEnvironmentVariable("INFO_EMAIL_ADDRESS");
            if (!string.IsNullOrWhiteSpace(u))
                options.UserName = u.Trim();
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            var p = Environment.GetEnvironmentVariable("INFO_EMAIL_PASSWORD");
            if (!string.IsNullOrWhiteSpace(p))
                options.Password = p;
        }

        var portEnv = Environment.GetEnvironmentVariable("SMTP_PORT_EMAIL");
        if (int.TryParse(portEnv, out var port) && port > 0 && port <= 65535)
            options.Port = port;
    }
}
