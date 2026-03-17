using CargoHub.Application.Auth.Abstractions;
using CargoHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Auth;

/// <summary>
/// Implementation of user registration using ASP.NET Core Identity.
/// </summary>
public sealed class UserRegistrationService : IUserRegistrationService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRegistrationService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<(string userId, string email, string displayName, string? businessId, string? customerMappingId)> CreateUserAsync(
        string email,
        string password,
        string userName,
        string? businessId,
        string? gsOne,
        CancellationToken cancellationToken = default)
    {
        // Store both email and userName so login works with either (account = email or userName)
        var user = new ApplicationUser
        {
            UserName = string.IsNullOrWhiteSpace(userName) ? email : userName,
            Email = email,
            DisplayName = userName,
            BusinessId = businessId,
            GsOne = gsOne
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(";", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to register user: {errors}");
        }

        // Single user identity: use same Id as CustomerMappingId so portal can send it as customer-id header
        user.CustomerMappingId = user.Id;
        await _userManager.UpdateAsync(user);

        // Assign default role for portal users (super admin is created via bootstrap only)
        await _userManager.AddToRoleAsync(user, Application.Auth.RoleNames.User);

        return (user.Id, user.Email ?? string.Empty, user.DisplayName, user.BusinessId, user.CustomerMappingId);
    }
}
