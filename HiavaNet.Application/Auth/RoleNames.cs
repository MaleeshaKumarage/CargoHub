namespace HiavaNet.Application.Auth;

/// <summary>
/// Well-known role names. Use these for [Authorize(Roles = ...)] and when assigning roles.
/// </summary>
public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string User = "User";
}
