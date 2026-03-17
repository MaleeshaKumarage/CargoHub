using HiavaNet.Application.Auth.Abstractions;
using HiavaNet.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace HiavaNet.Infrastructure.Auth;

public sealed class VerifyEmailRunner : IVerifyEmailRunner
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IVerificationCodeStore _store;

    public VerifyEmailRunner(UserManager<ApplicationUser> userManager, IVerificationCodeStore store)
    {
        _userManager = userManager;
        _store = store;
    }

    public async Task<(bool success, string? errorCode, string? message)> RunAsync(string code, CancellationToken cancellationToken = default)
    {
        if (!_store.TryGet(code, out var userId) || string.IsNullOrEmpty(userId))
            return (false, "BadRequest", "Invalid or expired code.");
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "NotFound", "User not found.");
        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);
        _store.Remove(code);
        return (true, null, "OK");
    }
}
