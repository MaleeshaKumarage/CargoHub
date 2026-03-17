namespace CargoHub.Application.Auth.Abstractions;

/// <summary>
/// Stores email verification code (e.g. Identity token) so verify can resolve user.
/// Implemented in Infrastructure.
/// </summary>
public interface IVerificationCodeStore
{
    void Store(string userId, string code, DateTimeOffset expiresAt);
    bool TryGet(string code, out string? userId);
    void Remove(string code);
}
