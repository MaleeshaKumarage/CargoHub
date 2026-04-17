namespace CargoHub.Domain.FreelanceRiders;

/// <summary>Token invite for a rider to create an account (similar to company admin invites).</summary>
public class FreelanceRiderInvite
{
    public Guid Id { get; set; }

    public Guid FreelanceRiderId { get; set; }

    public FreelanceRider? FreelanceRider { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>SHA-256 hex of raw token.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
