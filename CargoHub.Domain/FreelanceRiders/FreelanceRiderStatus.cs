namespace CargoHub.Domain.FreelanceRiders;

/// <summary>Lifecycle of a freelance rider record (invite → active).</summary>
public enum FreelanceRiderStatus
{
    PendingInvite = 0,
    Active = 1,
    Inactive = 2,
}
