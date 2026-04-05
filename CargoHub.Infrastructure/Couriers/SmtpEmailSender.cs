using CargoHub.Application.Couriers;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace CargoHub.Infrastructure.Couriers;

/// <summary>
/// Sends email via SMTP. Configure via SmtpOptions (e.g. from appsettings or env, aligned with booking-backend .env SMTP_*).
/// Uses MailKit so STARTTLS (port 587) and Gmail behave reliably; <see cref="System.Net.Mail.SmtpClient"/> is often flaky here.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpEmailSender(IOptions<SmtpOptions> options)
    {
        _options = options?.Value ?? new SmtpOptions();
    }

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
        SendAsync(to, subject, htmlBody, Array.Empty<EmailAttachment>(), cancellationToken, null);

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        IReadOnlyList<EmailAttachment> attachments,
        CancellationToken cancellationToken = default,
        string? plainTextBody = null)
    {
        if (string.IsNullOrEmpty(_options.Host))
            throw new InvalidOperationException(
                "SmtpOptions.Host is not configured. Set Smtp:Host in appsettings, or environment variable Smtp__Host, " +
                "or legacy SMTP_SERVER_EMAIL / SMTP_HOST. If you use a .env file, remove Smtp__Host when unused (an empty value overrides appsettings).");

        var from = _options.FromAddress;
        if (string.IsNullOrEmpty(from))
            throw new InvalidOperationException("SmtpOptions.FromAddress is not configured. Set Smtp:FromAddress in appsettings or environment.");

        var hasUser = !string.IsNullOrWhiteSpace(_options.UserName);
        if (hasUser && string.IsNullOrEmpty(_options.Password))
            throw new InvalidOperationException(
                "SmtpOptions.Password is empty but UserName is set. Gmail and most providers require a password (for Google, use an App Password with 2-Step Verification, not your normal login password). " +
                "If you deploy with GitHub Actions, ensure the Smtp__Password secret is set.");

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        if (!string.IsNullOrEmpty(plainTextBody))
            builder.TextBody = plainTextBody;
        foreach (var a in attachments)
        {
            if (a.Content.Length == 0) continue;
            var ct = ContentType.Parse(a.ContentType);
            builder.Attachments.Add(a.FileName, a.Content, ct);
        }

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        var secure = ResolveSecureSocketOptions();
        await client.ConnectAsync(_options.Host, _options.Port, secure, cancellationToken);

        if (hasUser)
            await client.AuthenticateAsync(_options.UserName!, _options.Password ?? string.Empty, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private SecureSocketOptions ResolveSecureSocketOptions()
    {
        // 465 = implicit TLS; 587/25 typically STARTTLS when UseSsl is true
        if (_options.Port == 465 && _options.UseSsl)
            return SecureSocketOptions.SslOnConnect;

        if (_options.UseSsl)
            return SecureSocketOptions.StartTls;

        return SecureSocketOptions.None;
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
