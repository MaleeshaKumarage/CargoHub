namespace CargoHub.Application.AdminEmail;

public sealed class ReleaseNotesSendFailure
{
    public string Email { get; init; } = "";
    public string Message { get; init; } = "";
}

public sealed class ReleaseNotesBroadcastResult
{
    public int RecipientCount { get; init; }
    public int SentCount { get; init; }
    public IReadOnlyList<ReleaseNotesSendFailure> Failures { get; init; } = Array.Empty<ReleaseNotesSendFailure>();
}
