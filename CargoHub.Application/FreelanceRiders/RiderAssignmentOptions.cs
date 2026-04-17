namespace CargoHub.Application.FreelanceRiders;

/// <summary>Configuration for freelance rider acceptance window (minutes).</summary>
public sealed class RiderAssignmentOptions
{
    public const string SectionName = "RiderAssignment";

    /// <summary>Minutes the rider has to accept before assignment lapses.</summary>
    public int AcceptanceWindowMinutes { get; set; } = 10;
}
