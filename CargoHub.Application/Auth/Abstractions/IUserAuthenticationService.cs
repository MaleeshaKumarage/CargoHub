using System.Security.Claims;

namespace CargoHub.Application.Auth.Abstractions;

/// <summary>
/// Service for authenticating users and validating credentials.
/// Encapsulates authentication logic to avoid Service Locator anti-pattern.
/// </summary>
public interface IUserAuthenticationService
{
    /// <summary>
    /// Validates user credentials and returns authentication result.
    /// </summary>
    /// <param name="account">Email, username, or display name</param>
    /// <param name="password">User's password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with user details and roles if successful</returns>
    Task<AuthenticationResult> ValidateCredentialsAsync(
        string account,
        string password,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of credential validation attempt.
/// </summary>
public sealed class AuthenticationResult
{
    /// <summary>
    /// Whether authentication was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// User ID if authentication succeeded.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's display name.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// User's company business ID if associated.
    /// </summary>
    public string? BusinessId { get; init; }

    /// <summary>
    /// Customer mapping ID for portal integration.
    /// </summary>
    public string? CustomerMappingId { get; init; }

    /// <summary>
    /// User's assigned roles.
    /// </summary>
    public IList<string> Roles { get; init; } = new List<string>();

    /// <summary>When <see cref="Success"/> is false, optional machine-readable reason (e.g. CompanyInactive).</summary>
    public string? ErrorCode { get; init; }

    /// <summary>When <see cref="Success"/> is false, optional user-facing message.</summary>
    public string? Message { get; init; }

    /// <summary>
    /// Creates a failed authentication result.
    /// </summary>
    public static AuthenticationResult Failed(string? errorCode = null, string? message = null) =>
        new() { Success = false, ErrorCode = errorCode, Message = message };

    /// <summary>
    /// Creates a successful authentication result.
    /// </summary>
    public static AuthenticationResult Succeeded(
        string userId,
        string email,
        string displayName,
        string? businessId,
        string? customerMappingId,
        IList<string> roles) =>
        new()
        {
            Success = true,
            UserId = userId,
            Email = email,
            DisplayName = displayName,
            BusinessId = businessId,
            CustomerMappingId = customerMappingId,
            Roles = roles
        };
}
