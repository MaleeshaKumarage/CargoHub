using System.Collections.Concurrent;
using HiavaNet.Application.Auth.Abstractions;

namespace HiavaNet.Infrastructure.Auth;

public sealed class VerificationCodeStore : IVerificationCodeStore
{
    private readonly ConcurrentDictionary<string, (string UserId, DateTimeOffset ExpiresAt)> _store = new();

    public void Store(string userId, string code, DateTimeOffset expiresAt)
    {
        _store[code] = (userId, expiresAt);
    }

    public bool TryGet(string code, out string? userId)
    {
        userId = null;
        if (!_store.TryGetValue(code, out var entry))
            return false;
        if (DateTimeOffset.UtcNow > entry.ExpiresAt)
        {
            _store.TryRemove(code, out _);
            return false;
        }
        userId = entry.UserId;
        return true;
    }

    public void Remove(string code)
    {
        _store.TryRemove(code, out _);
    }
}
