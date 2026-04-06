using CargoHub.Application.Auth;
using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Company;
using CargoHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Auth;

/// <summary>
/// Implementation of user authentication using ASP.NET Core Identity.
/// </summary>
public sealed class UserAuthenticationService : IUserAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ICompanyRepository _companies;

    public UserAuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ICompanyRepository companies)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _companies = companies;
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
        if (roles.Contains(RoleNames.SuperAdmin))
        {
            return AuthenticationResult.Succeeded(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                user.BusinessId,
                user.CustomerMappingId,
                roles);
        }

        var bid = user.BusinessId?.Trim();
        if (!string.IsNullOrEmpty(bid))
        {
            var company = await _companies.GetByBusinessIdAsync(bid, cancellationToken);
            if (company is { IsActive: false })
            {
                return AuthenticationResult.Failed(
                    "CompanyInactive",
                    AuthMessages.CompanyInactive);
            }
        }

        return AuthenticationResult.Succeeded(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.BusinessId,
            user.CustomerMappingId,
            roles);
    }
}
