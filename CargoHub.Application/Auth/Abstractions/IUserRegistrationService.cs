using CargoHub.Application.Auth.Dtos;

namespace CargoHub.Application.Auth.Abstractions;

/// <summary>
/// Service for registering new users in the system.
/// Encapsulates user creation logic to avoid Service Locator anti-pattern.
/// </summary>
public interface IUserRegistrationService
{
    /// <summary>
    /// Creates a new user with the specified credentials and company association.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="password">User's password</param>
    /// <param name="userName">Display name for the user</param>
    /// <param name="businessId">Optional company business ID</param>
    /// <param name="gsOne">Optional GS1 identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing user details: (userId, email, displayName, businessId, customerMappingId)</returns>
    Task<(string userId, string email, string displayName, string? businessId, string? customerMappingId)> CreateUserAsync(
        string email,
        string password,
        string userName,
        string? businessId,
        string? gsOne,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a user and assigns <paramref name="portalRole"/> (Admin or User).</summary>
    Task<(string userId, string email, string displayName, string? businessId, string? customerMappingId)> CreateUserAsync(
        string email,
        string password,
        string userName,
        string? businessId,
        string? gsOne,
        string portalRole,
        CancellationToken cancellationToken = default);
}
