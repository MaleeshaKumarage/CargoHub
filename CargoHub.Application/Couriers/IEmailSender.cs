namespace CargoHub.Application.Couriers;

/// <summary>
/// Sends an email. Implemented in Infrastructure (e.g. SMTP).
/// Used by email-based courier clients (e.g. Hämeen Tavarataxi, Scanlink).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
