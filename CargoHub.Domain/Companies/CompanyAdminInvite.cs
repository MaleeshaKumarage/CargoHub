namespace CargoHub.Domain.Companies;

/// <summary>
/// One-time invite for a user to become company Admin (set password / accept promotion).
/// </summary>
public class CompanyAdminInvite
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public Company? Company { get; set; }

    /// <summary>Original email address (display).</summary>
    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>SHA-256 hex (lowercase) of the raw token bytes.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
