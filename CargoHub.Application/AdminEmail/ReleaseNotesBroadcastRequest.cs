namespace CargoHub.Application.AdminEmail;

public sealed class ReleaseNotesBroadcastRequest
{
    public string Subject { get; set; } = "";
    public string BodyPlain { get; set; } = "";
    public bool AllCompanies { get; set; }
    /// <summary>When <see cref="AllCompanies"/> is false, non-empty list of company row ids (from admin company list).</summary>
    public IReadOnlyList<Guid>? CompanyIds { get; set; }
    public bool AllRoles { get; set; }
    /// <summary>When <see cref="AllRoles"/> is false, subset of SuperAdmin, Admin, User.</summary>
    public IReadOnlyList<string>? Roles { get; set; }
}
