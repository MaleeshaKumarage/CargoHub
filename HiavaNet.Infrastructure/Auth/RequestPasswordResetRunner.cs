using HiavaNet.Application.Auth.Abstractions;
using HiavaNet.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace HiavaNet.Infrastructure.Auth;

public sealed class RequestPasswordResetRunner : IRequestPasswordResetRunner
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPasswordResetTokenStore _store;

    public RequestPasswordResetRunner(UserManager<ApplicationUser> userManager, IPasswordResetTokenStore store)
    {
        _userManager = userManager;
        _store = store;
    }

    public async Task<(bool success, string? errorCode, string? message)> RunAsync(string email, string? env, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return (false, "NotFound", "User account not found.");
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        _store.Store(user.Id, token, expiresAt);
        return (true, null, "Success");
    }
}
