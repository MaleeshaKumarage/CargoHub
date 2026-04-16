namespace CargoHub.Domain.FreelanceRiders;

/// <summary>Normalized postal code served by a rider.</summary>
public class FreelanceRiderServiceArea
{
    public Guid Id { get; set; }

    public Guid FreelanceRiderId { get; set; }

    public FreelanceRider? FreelanceRider { get; set; }

    public string PostalCodeNormalized { get; set; } = string.Empty;
}
