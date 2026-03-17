using HiavaNet.Application.Auth.Abstractions;
using HiavaNet.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HiavaNet.Infrastructure.Auth;

/// <summary>
/// Implementation of user authentication using ASP.NET Core Identity.
/// </summary>
public sealed class UserAuthenticationService : IUserAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public UserAuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<AuthenticationResult> ValidateCredentialsAsync(
        string account,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(account)
                   ?? await _userManager.FindByNameAsync(account)
                   ?? await _userManager.Users.FirstOrDefaultAsync(
                          u => u.DisplayName != null && u.DisplayName.ToLower() == account.ToLower(),
                          cancellationToken);

        if (user == null) return AuthenticationResult.Failed();
        if (!user.IsActive) return AuthenticationResult.Failed();

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!result.Succeeded) return AuthenticationResult.Failed();

        var roles = await _userManager.GetRolesAsync(user);
        return AuthenticationResult.Succeeded(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.BusinessId,
            user.CustomerMappingId,
            roles);
    }
}
