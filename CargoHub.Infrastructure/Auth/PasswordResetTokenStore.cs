using System.Collections.Concurrent;
using CargoHub.Application.Auth.Abstractions;

namespace CargoHub.Infrastructure.Auth;

/// <summary>
/// In-memory store for password reset tokens. For production consider distributed cache.
/// </summary>
public sealed class PasswordResetTokenStore : IPasswordResetTokenStore
{
    private readonly ConcurrentDictionary<string, (string UserId, DateTimeOffset ExpiresAt)> _store = new();

    public void Store(string userId, string token, DateTimeOffset expiresAt)
    {
        _store[token] = (userId, expiresAt);
    }

    public bool TryGet(string token, out string? userId)
    {
        userId = null;
        if (!_store.TryGetValue(token, out var entry))
            return false;
        if (DateTimeOffset.UtcNow > entry.ExpiresAt)
        {
            _store.TryRemove(token, out _);
            return false;
        }
        userId = entry.UserId;
        return true;
    }

    public void Remove(string token)
    {
        _store.TryRemove(token, out _);
    }
}
