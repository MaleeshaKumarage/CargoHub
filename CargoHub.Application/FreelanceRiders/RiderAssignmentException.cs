namespace CargoHub.Application.FreelanceRiders;

/// <summary>Thrown when freelance rider validation fails during booking create/confirm.</summary>
public sealed class RiderAssignmentException : Exception
{
    public string ErrorCode { get; }

    public RiderAssignmentException(string errorCode, string message) : base(message) =>
        ErrorCode = errorCode;
}
