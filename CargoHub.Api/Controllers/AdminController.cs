using CargoHub.Application.AdminCompanies;
using CargoHub.Application.Auth;
using CargoHub.Application.Company;
using CargoHub.Application.Couriers;
using CargoHub.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Api.Controllers;

/// <summary>
/// SuperAdmin-only endpoints: list users by company, set role, activate/deactivate users.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = RoleNames.SuperAdmin)]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICompanyUserMetrics _companyUserMetrics;
    private readonly AdminCompanyUserPolicy _companyUserPolicy;
    private readonly IMediator _mediator;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        ICompanyRepository companyRepository,
        ICompanyUserMetrics companyUserMetrics,
        AdminCompanyUserPolicy companyUserPolicy,
        IMediator mediator)
    {
        _userManager = userManager;
        _companyRepository = companyRepository;
        _companyUserMetrics = companyUserMetrics;
        _companyUserPolicy = companyUserPolicy;
        _mediator = mediator;
    }

    /// <summary>
    /// List all companies (by distinct BusinessId from companies table). For each company you can then fetch users via GET /users?businessId=.
    /// </summary>
    [HttpGet("companies")]
    public async Task<ActionResult<List<CompanySummaryDto>>> GetCompanies(CancellationToken cancellationToken)
    {
        // Return companies from Company table so super admin sees which companies exist and can list users per company.
        var companies = await _companyRepository.GetAllAsync(cancellationToken);
        var list = new List<CompanySummaryDto>();
        foreach (var c in companies)
        {
            var bid = c.BusinessId ?? "";
            var users = string.IsNullOrEmpty(bid) ? 0 : await _companyUserMetrics.CountActiveUsersForBusinessIdAsync(bid, cancellationToken);
            var admins = string.IsNullOrEmpty(bid) ? 0 : await _companyUserMetrics.CountAdminsForBusinessIdAsync(bid, cancellationToken);
            list.Add(new CompanySummaryDto
            {
                Id = c.Id,
                Name = c.Name,
                BusinessId = c.BusinessId,
                CompanyId = c.CompanyId,
                MaxUserAccounts = c.MaxUserAccounts,
                MaxAdminAccounts = c.MaxAdminAccounts,
                InitialAdminInviteEmail = c.InitialAdminInviteEmail,
                ActiveUserCount = users,
                AdminCount = admins
            });
        }

        return Ok(list);
    }

    /// <summary>Create a company with limits and send initial admin invite (explicit or fallback email).</summary>
    [HttpPost("companies")]
    public async Task<ActionResult<AdminCompanyDetailDto>> CreateCompany([FromBody] CreateAdminCompanyRequest body, CancellationToken cancellationToken)
    {
        var initialEmails = MergeInitialAdminEmails(body);
        var result = await _mediator.Send(
            new CreateAdminCompanyCommand(body.Name, body.BusinessId, body.MaxUserAccounts, body.MaxAdminAccounts, initialEmails),
            cancellationToken);
        if (!result.Success)
            return BadRequest(new { errorCode = result.ErrorCode, message = result.Message });
        var created = result.Company!;
        return Created($"/api/v1/admin/companies/{created.Id}", created);
    }

    /// <summary>Update company limits and optionally resend admin invite when there is still no admin.</summary>
    [HttpPatch("companies/{id:guid}")]
    public async Task<ActionResult<AdminCompanyDetailDto>> PatchCompany(Guid id, [FromBody] PatchAdminCompanyRequest body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateAdminCompanyCommand(id, body.MaxUserAccounts, body.MaxAdminAccounts, body.ResendAdminInvite),
            cancellationToken);
        if (!result.Success)
            return BadRequest(new { errorCode = result.ErrorCode, message = result.Message });
        return Ok(result.Company);
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

        if (request.Role != null)
        {
            var role = request.Role.Trim();
            var validRoles = new[] { RoleNames.SuperAdmin, RoleNames.Admin, RoleNames.User };
            if (!validRoles.Contains(role))
                return BadRequest(new { message = "Invalid role. Use SuperAdmin, Admin, or User." });
        }

        var policyError = await _companyUserPolicy.ValidatePatchAsync(user, request.Role?.Trim(), request.IsActive, cancellationToken);
        if (policyError != null)
            return BadRequest(new { message = policyError });

        if (request.Role != null)
        {
            var role = request.Role!.Trim();
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
            await _userManager.UpdateAsync(user);
        }

        return NoContent();
    }

    /// <summary>Send a simple HTML message to verify SMTP (Super Admin only).</summary>
    [HttpPost("email/test")]
    public async Task<ActionResult> SendTestEmail([FromBody] TestEmailRequest body, [FromServices] IEmailSender emailSender, CancellationToken cancellationToken)
    {
        var to = body.To?.Trim() ?? "";
        if (string.IsNullOrEmpty(to))
            return BadRequest(new { message = "Recipient address (to) is required." });
        try
        {
            await emailSender.SendAsync(
                to,
                "CargoHub — email configuration test",
                "<p>If you received this message, outbound SMTP from the API is working.</p>",
                cancellationToken);
            return Ok(new { ok = true, message = "Test email sent." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static List<string>? MergeInitialAdminEmails(CreateAdminCompanyRequest body)
    {
        var fromList = CompanyAdminInviteEmailsHelper.NormalizeList(body.InitialAdminEmails);
        if (fromList.Count > 0)
            return fromList;
        if (!string.IsNullOrWhiteSpace(body.InitialAdminEmail))
            return new List<string> { body.InitialAdminEmail.Trim() };
        return null;
    }

    public sealed class CompanySummaryDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? BusinessId { get; set; }
        public string CompanyId { get; set; } = "";
        public int? MaxUserAccounts { get; set; }
        public int? MaxAdminAccounts { get; set; }
        public string? InitialAdminInviteEmail { get; set; }
        public List<string>? InitialAdminInviteEmails { get; set; }
        public int ActiveUserCount { get; set; }
        public int AdminCount { get; set; }
    }

    public sealed class CreateAdminCompanyRequest
    {
        public string Name { get; set; } = "";
        public string BusinessId { get; set; } = "";
        public int? MaxUserAccounts { get; set; }
        public int? MaxAdminAccounts { get; set; }
        /// <summary>Legacy single email; ignored when <see cref="InitialAdminEmails"/> is non-empty.</summary>
        public string? InitialAdminEmail { get; set; }
        public List<string>? InitialAdminEmails { get; set; }
    }

    public sealed class TestEmailRequest
    {
        public string? To { get; set; }
    }

    public sealed class PatchAdminCompanyRequest
    {
        public int? MaxUserAccounts { get; set; }
        public int? MaxAdminAccounts { get; set; }
        public bool ResendAdminInvite { get; set; }
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
