using HiavaNet.Application.Auth.Abstractions;
using HiavaNet.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace HiavaNet.Infrastructure.Auth;

public sealed class UpdateVerificationStatusRunner : IUpdateVerificationStatusRunner
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateVerificationStatusRunner(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<(bool success, string? errorCode, string? message)> RunAsync(string userId, string verificationStatus, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return (false, "NotFound", "User not found.");
        user.EmailConfirmed = string.Equals(verificationStatus, "verified", StringComparison.OrdinalIgnoreCase);
        await _userManager.UpdateAsync(user);
        return (true, null, "OK");
    }
}
