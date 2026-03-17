using HiavaNet.Application.Auth.Abstractions;
using HiavaNet.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace HiavaNet.Infrastructure.Auth;

public sealed class ResetPasswordRunner : IResetPasswordRunner
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPasswordResetTokenStore _store;

    public ResetPasswordRunner(UserManager<ApplicationUser> userManager, IPasswordResetTokenStore store)
    {
        _userManager = userManager;
        _store = store;
    }

    public async Task<(bool success, string? errorCode, string? message)> RunAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        if (!_store.TryGet(token, out var userId) || string.IsNullOrEmpty(userId))
            return (false, "BadRequest", "Invalid or expired token.");
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "NotFound", "User account not found.");
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        _store.Remove(token);
        if (!result.Succeeded)
            return (false, "BadRequest", string.Join(";", result.Errors.Select(e => e.Description)));
        return (true, null, "Success.");
    }
}
