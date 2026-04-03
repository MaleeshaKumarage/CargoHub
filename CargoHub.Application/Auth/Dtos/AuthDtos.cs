using System.Text.Json.Serialization;

namespace CargoHub.Application.Auth.Dtos;

/// <summary>
/// Request body for portal login.
/// Matches the shape used by the existing portal backend.
/// </summary>
public sealed class PortalLoginRequest
{
    public string Account { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request body for portal user registration.
/// </summary>
public sealed class PortalRegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? BusinessId { get; set; }
    public string? GsOne { get; set; }
}

/// <summary>
/// Minimal auth response returned after successful login.
/// </summary>
public sealed class LoginResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? BusinessId { get; set; }
    public string? CustomerMappingId { get; set; }
    public string JwtToken { get; set; } = string.Empty;
    /// <summary>Role names (e.g. SuperAdmin, Admin, User) for UI and authorization.</summary>
    [JsonPropertyName("roles")]
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Result of a login attempt. Use Success and Data when ok; ErrorCode and Message when failed.
/// </summary>
public sealed class LoginResult
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public LoginResponse? Data { get; set; }
}

/// <summary>Current user info from GET /api/v1/portal/me (roles from JWT claims; businessId and companyName from user's company).</summary>
public sealed class PortalMeResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    [JsonPropertyName("roles")]
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    /// <summary>User's company business ID (e.g. government ID). Empty if user has no company.</summary>
    public string? BusinessId { get; set; }
    /// <summary>User's company display name. Empty if user has no company.</summary>
    public string? CompanyName { get; set; }

    /// <summary>UI design theme preference (skeuomorphism, neobrutalism, claymorphism, minimalism).</summary>
    public string? Theme { get; set; }
}

/// <summary>Request for PATCH /api/v1/portal/me/preferences. Body: { theme }.</summary>
public sealed class UpdatePreferencesRequest
{
    public string Theme { get; set; } = string.Empty;
}

/// <summary>Request for portal requestPasswordReset. Body: { email }.</summary>
public sealed class RequestPasswordResetRequest
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>Request for portal resetPassword. Body: { token, newPassword }.</summary>
public sealed class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>Request for POST /api/v1/portal/accept-company-admin-invite. Body: { token, password, userName }.</summary>
public sealed class AcceptCompanyAdminInviteRequest
{
    public string Token { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

/// <summary>Request for portal verify. Body: { code }.</summary>
public sealed class VerifyRequest
{
    public string Code { get; set; } = string.Empty;
}

/// <summary>Request for portal update-status. Body: { userID, verification_status }.</summary>
public sealed class UpdateVerificationStatusRequest
{
    public string UserID { get; set; } = string.Empty;
    public string Verification_status { get; set; } = "verified"; // "verified" | "not_verified"
}

/// <summary>Generic result for auth operations that return status + message.</summary>
public sealed class AuthResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; } // e.g. "NotFound", "BadRequest"
}

/// <summary>Result of registration: either success with login response or error message.</summary>
public sealed class RegisterResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public LoginResponse? Data { get; set; }
}

