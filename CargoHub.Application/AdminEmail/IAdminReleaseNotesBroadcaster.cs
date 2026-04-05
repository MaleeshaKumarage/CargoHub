namespace CargoHub.Application.AdminEmail;

public interface IAdminReleaseNotesBroadcaster
{
    /// <summary>
    /// Resolves recipients and sends one email per distinct address. Returns null and an error message when validation fails (unknown companies, invalid roles, etc.).
    /// </summary>
    Task<(ReleaseNotesBroadcastResult? Result, string? ErrorMessage)> TryBroadcastAsync(
        ReleaseNotesBroadcastRequest request,
        CancellationToken cancellationToken = default);
}
