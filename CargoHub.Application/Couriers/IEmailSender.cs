namespace CargoHub.Application.Couriers;

/// <summary>
/// Sends an email. Implemented in Infrastructure (e.g. SMTP).
/// Used by email-based courier clients (e.g. Hämeen Tavarataxi, Scanlink).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);

    /// <param name="attachments">Optional PDFs etc.; empty collection sends HTML only.</param>
    /// <param name="plainTextBody">When set, sent as multipart/alternative with HTML (better for clients that prefer plain text).</param>
    Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        IReadOnlyList<EmailAttachment> attachments,
        CancellationToken cancellationToken = default,
        string? plainTextBody = null);
}
