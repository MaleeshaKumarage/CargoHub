using CargoHub.Application.Auth;
using CargoHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CargoHub.Api;

/// <summary>
/// Clears SuperAdmin role assignments so <see cref="Controllers.BootstrapController.BootstrapSuperAdmin"/> can run again.
/// Same deployment secret as bootstrap; use only for recovery / dev environments.
/// </summary>
public static class BootstrapSuperAdminReset
{
    /// <param name="deleteSuperAdminUsers">When true, deletes each user that had SuperAdmin (frees email for a new bootstrap). When false, only removes the role.</param>
    public static async Task<(int SuperAdminsCleared, int SuperAdminUsersDeleted)> ExecuteAsync(
        UserManager<ApplicationUser> userManager,
        bool deleteSuperAdminUsers,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var superAdmins = (await userManager.GetUsersInRoleAsync(RoleNames.SuperAdmin)).ToList();
        if (superAdmins.Count == 0)
            return (0, 0);

        if (deleteSuperAdminUsers)
        {
            var deleted = 0;
            foreach (var u in superAdmins)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await userManager.DeleteAsync(u);
                if (result.Succeeded)
                    deleted++;
            }

            return (superAdmins.Count, deleted);
        }

        var cleared = 0;
        foreach (var u in superAdmins)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await userManager.RemoveFromRoleAsync(u, RoleNames.SuperAdmin);
            if (result.Succeeded)
                cleared++;
        }

        return (cleared, 0);
    }
}
