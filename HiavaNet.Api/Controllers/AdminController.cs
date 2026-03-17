using HiavaNet.Application.Auth;
using HiavaNet.Application.Company;
using HiavaNet.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HiavaNet.Api.Controllers;

/// <summary>
/// SuperAdmin-only endpoints: list users by company, set role, activate/deactivate users.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = RoleNames.SuperAdmin)]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ICompanyRepository _companyRepository;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ICompanyRepository companyRepository)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _companyRepository = companyRepository;
    }

    /// <summary>
    /// List all companies (by distinct BusinessId from companies table). For each company you can then fetch users via GET /users?businessId=.
    /// </summary>
    [HttpGet("companies")]
    public async Task<ActionResult<List<CompanySummaryDto>>> GetCompanies(CancellationToken cancellationToken)
    {
        // Return companies from Company table so super admin sees which companies exist and can list users per company.
        var companies = await _companyRepository.GetAllAsync(cancellationToken);
        var list = companies
            .Select(c => new CompanySummaryDto
            {
                Id = c.Id,
                Name = c.Name,
                BusinessId = c.BusinessId,
                CompanyId = c.CompanyId
            })
            .ToList();
        return Ok(list);
    }

    /// <summary>
    /// List users. Optional query: businessId to filter by company (government ID).
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers([FromQuery] string? businessId, CancellationToken cancellationToken)
    {
        var query = _userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(businessId))
        {
            var bid = businessId.Trim().ToLower();
            query = query.Where(u => u.BusinessId != null && u.BusinessId.Trim().ToLower() == bid);
        }

        var users = await query.ToListAsync(cancellationToken);
        var dtos = new List<AdminUserDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            dtos.Add(new AdminUserDto
            {
                UserId = u.Id,
                Email = u.Email ?? "",
                DisplayName = u.DisplayName ?? "",
                BusinessId = u.BusinessId,
                IsActive = u.IsActive,
                Roles = roles.ToList()
            });
        }
        return Ok(dtos);
    }

    /// <summary>
    /// Update a user's role and/or active status.
    /// </summary>
    [HttpPatch("users/{userId}")]
    public async Task<ActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
            await _userManager.UpdateAsync(user);
        }

        if (request.Role != null)
        {
            var role = request.Role.Trim();
            var validRoles = new[] { RoleNames.SuperAdmin, RoleNames.Admin, RoleNames.User };
            if (!validRoles.Contains(role))
                return BadRequest(new { message = "Invalid role. Use SuperAdmin, Admin, or User." });

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);
        }

        return NoContent();
    }

    public sealed class CompanySummaryDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? BusinessId { get; set; }
        public string CompanyId { get; set; } = "";
    }

    public sealed class AdminUserDto
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? BusinessId { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public sealed class UpdateUserRequest
    {
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
    }
}
