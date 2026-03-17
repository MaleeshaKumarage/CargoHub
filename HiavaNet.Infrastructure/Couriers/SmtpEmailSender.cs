using System.Net;
using System.Net.Mail;
using HiavaNet.Application.Couriers;
using Microsoft.Extensions.Options;

namespace HiavaNet.Infrastructure.Couriers;

/// <summary>
/// Sends email via SMTP. Configure via SmtpOptions (e.g. from appsettings or env, aligned with booking-backend .env SMTP_*).
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpEmailSender(IOptions<SmtpOptions> options)
    {
        _options = options?.Value ?? new SmtpOptions();
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.Host))
            throw new InvalidOperationException("SmtpOptions.Host is not configured.");

        using var client = new SmtpClient(_options.Host, _options.Port);
        client.EnableSsl = _options.UseSsl;
        if (!string.IsNullOrEmpty(_options.UserName))
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);

        var from = _options.FromAddress;
        if (string.IsNullOrEmpty(from))
            throw new InvalidOperationException("SmtpOptions.FromAddress is not configured. Set Smtp:FromAddress in appsettings or environment.");
        using var message = new MailMessage(from, to, subject, htmlBody) { IsBodyHtml = true };
        await client.SendMailAsync(message, cancellationToken);
    }
}

/// <summary>
/// SMTP configuration. Bind from Smtp or environment (SMTP_SERVER_EMAIL, SMTP_PORT_EMAIL, INFO_EMAIL_ADDRESS, INFO_EMAIL_PASSWORD).
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";
    public string? Host { get; set; }
    public int Port { get; set; } = 465;
    public bool UseSsl { get; set; } = true;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? FromAddress { get; set; }
}
