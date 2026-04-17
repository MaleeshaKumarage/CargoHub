using CargoHub.Domain.Companies;

namespace CargoHub.Domain.FreelanceRiders;

/// <summary>Platform freelance rider profile. Optional <see cref="CompanyId"/> limits visibility to that company only.</summary>
public class FreelanceRider
{
    public Guid Id { get; set; }

    /// <summary>Rider's own government / business identifier (not CargoHub company).</summary>
    public string BusinessId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    /// <summary>Contact email (display).</summary>
    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public FreelanceRiderStatus Status { get; set; } = FreelanceRiderStatus.PendingInvite;

    /// <summary>Linked Identity user after invite acceptance.</summary>
    public string? ApplicationUserId { get; set; }

    /// <summary>
    /// When set, this rider appears in matching only for bookings of this company.
    /// When null, rider is global (all companies).
    /// </summary>
    public Guid? CompanyId { get; set; }

    public Company? Company { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<FreelanceRiderServiceArea> ServiceAreas { get; set; } = new List<FreelanceRiderServiceArea>();
}
