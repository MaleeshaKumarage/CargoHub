using System.ComponentModel.DataAnnotations;
using CargoHub.Application.Auth;
using CargoHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CargoHub.Api.Controllers;

/// <summary>
/// One-time bootstrap endpoint to create the first SuperAdmin user.
/// Secured by a secret (from configuration); not available for normal registration.
/// </summary>
[ApiController]
[Route("api/v1/portal")]
[AllowAnonymous]
public class BootstrapController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public BootstrapController(
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _configuration = configuration;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// Creates the first SuperAdmin user. Requires header X-Bootstrap-Secret to match configuration (Bootstrap:Secret).
    /// Fails if any user already has the SuperAdmin role. Call once during initial deployment.
    /// </summary>
    [HttpPost("bootstrap-superadmin")]
    public async Task<ActionResult<BootstrapResponse>> BootstrapSuperAdmin([FromBody] BootstrapRequest request, CancellationToken cancellationToken)
    {
        var secret = _configuration["Bootstrap:Secret"];
        if (string.IsNullOrEmpty(secret))
            return StatusCode(500, new { message = "Bootstrap is not configured (Bootstrap:Secret)." });

        var providedSecret = Request.Headers["X-Bootstrap-Secret"].FirstOrDefault();
        if (providedSecret != secret)
            return Forbid();

        var superAdmins = await _userManager.GetUsersInRoleAsync(RoleNames.SuperAdmin);
        if (superAdmins.Count > 0)
            return BadRequest(new { message = "A SuperAdmin user already exists. Bootstrap can only run once." });

        var email = request.Email?.Trim() ?? "";
        var password = request.Password ?? "";
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            return BadRequest(new { message = "Email and password are required." });

        var displayName = request.DisplayName?.Trim() ?? email;
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = "Failed to create user.", errors });
        }

        user.CustomerMappingId = user.Id;
        await _userManager.UpdateAsync(user);

        await _userManager.AddToRoleAsync(user, RoleNames.SuperAdmin);

        return CreatedAtAction(nameof(BootstrapSuperAdmin), new BootstrapResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName,
            Message = "SuperAdmin created. Use the same email/password to log in. Change the password after first login if desired."
        });
    }

    /// <summary>
    /// Removes all SuperAdmin role assignments (or deletes those users) so bootstrap can run again.
    /// Requires the same header <c>X-Bootstrap-Secret</c> as <see cref="BootstrapSuperAdmin"/>.
    /// Use <see cref="ResetBootstrapRequest.DeleteSuperAdminUsers"/> to delete accounts and free the email for a new bootstrap.
    /// </summary>
    [HttpPost("reset-bootstrap-superadmin")]
    public async Task<ActionResult<ResetBootstrapResponse>> ResetBootstrapSuperAdmin([FromBody] ResetBootstrapRequest? request, CancellationToken cancellationToken)
    {
        var secret = _configuration["Bootstrap:Secret"];
        if (string.IsNullOrEmpty(secret))
            return StatusCode(500, new { message = "Bootstrap is not configured (Bootstrap:Secret)." });

        var providedSecret = Request.Headers["X-Bootstrap-Secret"].FirstOrDefault();
        if (providedSecret != secret)
            return Forbid();

        var deleteUsers = request?.DeleteSuperAdminUsers ?? false;
        var (cleared, deleted) = await BootstrapSuperAdminReset.ExecuteAsync(_userManager, deleteUsers, cancellationToken);

        return Ok(new ResetBootstrapResponse
        {
            SuperAdminsCleared = cleared,
            SuperAdminUsersDeleted = deleted,
            Message = deleteUsers
                ? "SuperAdmin users processed. Deleted accounts where possible; you can run bootstrap-superadmin again (reuse email if delete succeeded)."
                : "SuperAdmin role removed from all matching users. Run bootstrap-superadmin to create a new SuperAdmin. Use deleteSuperAdminUsers: true if the same email must be reused."
        });
    }

    public sealed class BootstrapRequest
    {
        [Required]
        public string? Email { get; set; }
        [Required]
        [MinLength(6)]
        public string? Password { get; set; }
        public string? DisplayName { get; set; }
    }

    public sealed class BootstrapResponse
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public sealed class ResetBootstrapRequest
    {
        /// <summary>When true, deletes each user that had SuperAdmin (so bootstrap can reuse the same email).</summary>
        public bool DeleteSuperAdminUsers { get; set; }
    }

    public sealed class ResetBootstrapResponse
    {
        public int SuperAdminsCleared { get; set; }
        public int SuperAdminUsersDeleted { get; set; }
        public string Message { get; set; } = "";
    }
}
