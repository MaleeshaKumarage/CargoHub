namespace CargoHub.Application.Couriers;

/// <summary>Binary attachment for outbound email (e.g. PDF digest).</summary>
public sealed class EmailAttachment
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required byte[] Content { get; init; }
}
