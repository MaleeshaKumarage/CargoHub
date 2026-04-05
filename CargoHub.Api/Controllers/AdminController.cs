using System.Security.Claims;
using CargoHub.Application.AdminCompanies;
using CargoHub.Application.AdminEmail;
using CargoHub.Application.Auth;
using CargoHub.Application.Billing.Admin;
using CargoHub.Application.Billing.AdminInvoicing;
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
    private readonly IAdminReleaseNotesBroadcaster _releaseNotesBroadcaster;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        ICompanyRepository companyRepository,
        ICompanyUserMetrics companyUserMetrics,
        AdminCompanyUserPolicy companyUserPolicy,
        IMediator mediator,
        IAdminReleaseNotesBroadcaster releaseNotesBroadcaster)
    {
        _userManager = userManager;
        _companyRepository = companyRepository;
        _companyUserMetrics = companyUserMetrics;
        _companyUserPolicy = companyUserPolicy;
        _mediator = mediator;
        _releaseNotesBroadcaster = releaseNotesBroadcaster;
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
                AdminCount = admins,
                SubscriptionPlanId = c.SubscriptionPlanId
            });
        }

        return Ok(list);
    }

    /// <summary>List billing periods (UTC month buckets) for a company.</summary>
    [HttpGet("companies/{companyId:guid}/billing-periods")]
    public async Task<ActionResult<IReadOnlyList<CompanyBillingPeriodSummaryDto>>> GetCompanyBillingPeriods(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company == null)
            return NotFound(new { message = "Company not found." });
        var list = await _mediator.Send(new ListCompanyBillingPeriodsQuery(companyId), cancellationToken);
        return Ok(list);
    }

    /// <summary>Billing period detail with line items (Super Admin invoice-style view).</summary>
    [HttpGet("billing-periods/{periodId:guid}")]
    public async Task<ActionResult<BillingPeriodDetailDto>> GetBillingPeriodDetail(Guid periodId, CancellationToken cancellationToken)
    {
        var detail = await _mediator.Send(new GetBillingPeriodDetailQuery(periodId), cancellationToken);
        if (detail == null)
            return NotFound(new { message = "Billing period not found." });
        return Ok(detail);
    }

    /// <summary>Create a company with limits and send initial admin invite (explicit or fallback email).</summary>
    [HttpPost("companies")]
    public async Task<ActionResult<AdminCompanyDetailDto>> CreateCompany([FromBody] CreateAdminCompanyRequest body, CancellationToken cancellationToken)
    {
        var initialEmails = MergeInitialAdminEmails(body);
        var result = await _mediator.Send(
            new CreateAdminCompanyCommand(body.Name, body.BusinessId, body.MaxUserAccounts, body.MaxAdminAccounts, initialEmails, body.SubscriptionPlanId),
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
            new UpdateAdminCompanyCommand(
                id,
                body.MaxUserAccounts,
                body.MaxAdminAccounts,
                body.ResendAdminInvite,
                body.DeactivateUserIds,
                body.DemoteAdminUserIds,
                body.SubscriptionPlanId),
            cancellationToken);
        if (!result.Success)
        {
            if (result.ErrorCode == "LimitReductionRequired" && result.LimitReductionRequired is { } lr)
            {
                return Conflict(new
                {
                    errorCode = result.ErrorCode,
                    message = result.Message,
                    activeUserCount = lr.ActiveUserCount,
                    proposedMaxUserAccounts = lr.ProposedMaxUserAccounts,
                    adminCount = lr.AdminCount,
                    proposedMaxAdminAccounts = lr.ProposedMaxAdminAccounts,
                    minimumUsersToDeactivate = lr.MinimumUsersToDeactivate,
                    minimumAdminsToDemote = lr.MinimumAdminsToDemote,
                    businessId = lr.BusinessId
                });
            }

            return BadRequest(new { errorCode = result.ErrorCode, message = result.Message });
        }

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

    /// <summary>Broadcast release notes to users filtered by company and role (Super Admin only).</summary>
    [HttpPost("email/release-notes")]
    public async Task<ActionResult> SendReleaseNotes([FromBody] ReleaseNotesEmailRequest body, CancellationToken cancellationToken)
    {
        var req = new ReleaseNotesBroadcastRequest
        {
            Subject = body.Subject ?? "",
            BodyPlain = body.Body ?? "",
            AllCompanies = body.AllCompanies,
            CompanyIds = body.CompanyIds,
            AllRoles = body.AllRoles,
            Roles = body.Roles,
        };
        var (result, err) = await _releaseNotesBroadcaster.TryBroadcastAsync(req, cancellationToken);
        if (err != null || result == null)
            return BadRequest(new { message = err ?? "Broadcast failed." });
        return Ok(new
        {
            recipientCount = result.RecipientCount,
            sentCount = result.SentCount,
            failures = result.Failures.Select(f => new { email = f.Email, message = f.Message }).ToList(),
        });
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
        public Guid? SubscriptionPlanId { get; set; }
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
        public Guid? SubscriptionPlanId { get; set; }
    }

    public sealed class ReleaseNotesEmailRequest
    {
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public bool AllCompanies { get; set; }
        public List<Guid>? CompanyIds { get; set; }
        public bool AllRoles { get; set; }
        public List<string>? Roles { get; set; }
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
        public List<string>? DeactivateUserIds { get; set; }
        public List<string>? DemoteAdminUserIds { get; set; }
        public Guid? SubscriptionPlanId { get; set; }
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

    public sealed class SendInvoiceEmailRequest
    {
        public string? RecipientAdminUserId { get; set; }
    }

    public sealed class PatchBillingLineItemRequest
    {
        public bool ExcludedFromInvoice { get; set; }
    }
}
