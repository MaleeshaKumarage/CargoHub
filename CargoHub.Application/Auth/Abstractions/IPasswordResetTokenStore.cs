namespace CargoHub.Application.Auth.Abstractions;

/// <summary>
/// Stores password reset tokens (userId + token) so reset can resolve user from token.
/// Implemented in Infrastructure.
/// </summary>
public interface IPasswordResetTokenStore
{
    void Store(string userId, string token, DateTimeOffset expiresAt);
    bool TryGet(string token, out string? userId);
    void Remove(string token);
}
