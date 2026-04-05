using System.Security.Claims;
using CargoHub.Api.Options;
using CargoHub.Application.Auth;
using CargoHub.Application.Auth.Commands;
using CargoHub.Application.Auth.Dtos;
using CargoHub.Application.Company;
using CargoHub.Application.Couriers;
using CargoHub.Application.Subscriptions;
using CargoHub.Application.Subscriptions.Queries;
using CargoHub.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CargoHub.Api.Controllers;

/// <summary>
/// Single entry for portal user registration and login.
/// Uses the same user creation and login flow as the rest of the API (CQRS).
/// Compatible with portal (api/v1/portal paths).
/// </summary>
[ApiController]
[Route("api/v1/portal")]
public class PortalController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly BrandingOptions _branding;
    private readonly ICourierBookingClientFactory _courierFactory;
    private readonly ICompanyRepository _companyRepository;

    public PortalController(
        IMediator mediator,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOptions<BrandingOptions> branding,
        ICourierBookingClientFactory courierFactory,
        ICompanyRepository companyRepository)
    {
        _mediator = mediator;
        _userManager = userManager;
        _signInManager = signInManager;
        _branding = branding?.Value ?? new BrandingOptions();
        _courierFactory = courierFactory;
        _companyRepository = companyRepository;
    }

    /// <summary>Enabled courier IDs for the current company (booking dropdown). SuperAdmin receives full registered list.</summary>
    [HttpGet("couriers")]
    [Authorize]
    public async Task<ActionResult<CouriersResponse>> GetCouriers(CancellationToken cancellationToken)
    {
        if (User.IsInRole(RoleNames.SuperAdmin))
            return Ok(new CouriersResponse { CourierIds = _courierFactory.RegisteredCourierIds });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var user = await _userManager.FindByIdAsync(userId);
        if (string.IsNullOrWhiteSpace(user?.BusinessId))
            return Ok(new CouriersResponse { CourierIds = Array.Empty<string>() });

        var company = await _companyRepository.GetByBusinessIdAsync(user.BusinessId, cancellationToken);
        if (company == null)
            return Ok(new CouriersResponse { CourierIds = Array.Empty<string>() });

        var enabled = await _companyRepository.GetEnabledCourierIdsForCompanyAsync(company.Id, cancellationToken);
        var ordered = enabled.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        return Ok(new CouriersResponse { CourierIds = ordered });
    }

    /// <summary>All registered courier IDs for the company admin contract configuration UI.</summary>
    [HttpGet("couriers/catalog")]
    [Authorize(Roles = RoleNames.Admin)]
    public ActionResult<CouriersResponse> GetCourierCatalog()
    {
        return Ok(new CouriersResponse { CourierIds = _courierFactory.RegisteredCourierIds });
    }

    /// <summary>Returns deployment branding (app name, logo, colors) for the portal. No auth required.</summary>
    [HttpGet("branding")]
    [AllowAnonymous]
    public ActionResult<BrandingResponse> GetBranding()
    {
        return Ok(new BrandingResponse
        {
            AppName = _branding.AppName ?? "CargoHub",
            LogoUrl = _branding.LogoUrl ?? "",
            PrimaryColor = _branding.PrimaryColor ?? "",
            SecondaryColor = _branding.SecondaryColor ?? ""
        });
    }

    /// <summary>Accept company admin invitation (from email link). Creates account as Admin or promotes existing user.</summary>
    [HttpPost("accept-company-admin-invite")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> AcceptCompanyAdminInvite([FromBody] AcceptCompanyAdminInviteRequest request)
    {
        var result = await _mediator.Send(new AcceptCompanyAdminInviteCommand(request), HttpContext.RequestAborted);
        if (!result.Success)
            return BadRequest(new { errorCode = result.ErrorCode, message = result.Message });
        return Ok(result.Data);
    }

    /// <summary>
    /// Register a new user. Company ID (government business ID) is required and must match an existing company created by an administrator.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] PortalRegisterRequest request)
    {
        var result = await _mediator.Send(new RegisterUserCommand(request), HttpContext.RequestAborted);
        if (!result.Success)
            return BadRequest(new { errorCode = result.ErrorCode, message = result.Message });
        return Ok(result.Data);
    }

    /// <summary>
    /// Login (single login path). Same implementation as api/v1/auth/login.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] PortalLoginRequest request)
    {
        var result = await _mediator.Send(new LoginUserCommand(request), HttpContext.RequestAborted);

        if (!result.Success)
        {
            return Unauthorized(new { errorCode = result.ErrorCode, message = result.Message });
        }

        var user = await _userManager.FindByIdAsync(result.Data!.UserId);
        if (user != null)
        {
            await _signInManager.SignInAsync(user, isPersistent: true);
        }

        return Ok(result.Data);
    }

    /// <summary>Returns current user info and roles from JWT, plus company business ID and name when user is linked to a company.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CargoHub.Application.Auth.Dtos.PortalMeResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name);
        var displayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "";
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        string? businessId = null;
        string? companyName = null;
        var user = await _userManager.FindByIdAsync(userId);
        if (!string.IsNullOrWhiteSpace(user?.BusinessId))
        {
            businessId = user.BusinessId.Trim();
            var company = await _companyRepository.GetByBusinessIdAsync(businessId, cancellationToken);
            companyName = company?.Name;
        }

        return Ok(new CargoHub.Application.Auth.Dtos.PortalMeResponse
        {
            UserId = userId,
            Email = email ?? "",
            DisplayName = displayName,
            Roles = roles,
            BusinessId = businessId,
            CompanyName = companyName,
            Theme = user?.Theme ?? "minimalism"
        });
    }

    private static readonly HashSet<string> ValidThemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "skeuomorphism", "neobrutalism", "claymorphism", "minimalism"
    };

    /// <summary>Update current user preferences (e.g. theme). Body: { theme }.</summary>
    [HttpPatch("me/preferences")]
    [Authorize]
    public async Task<IActionResult> UpdatePreferences([FromBody] CargoHub.Application.Auth.Dtos.UpdatePreferencesRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Theme) || !ValidThemes.Contains(request.Theme.Trim()))
            return BadRequest(new { message = "Theme must be one of: skeuomorphism, neobrutalism, claymorphism, minimalism" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Unauthorized();

        user.Theme = request.Theme.Trim();
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return StatusCode(500, new { message = "Failed to update preferences" });

        return Ok(new { theme = user.Theme });
    }

    /// <summary>Current company subscription plan and pricing (active rate card at UTC now). User must be linked to a company.</summary>
    [HttpGet("company/subscription")]
    [Authorize]
    public async Task<ActionResult<PortalCompanySubscriptionDto>> GetCompanySubscription(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        var user = await _userManager.FindByIdAsync(userId);
        if (string.IsNullOrWhiteSpace(user?.BusinessId))
            return NotFound();
        var dto = await _mediator.Send(new GetPortalCompanySubscriptionQuery(user.BusinessId.Trim()), cancellationToken);
        if (dto == null)
            return NotFound();
        return Ok(dto);
    }

    /// <summary>Request password reset; sends token (email sending optional). Body: { email }.</summary>
    [HttpPost("requestPasswordReset")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> RequestPasswordReset([FromBody] RequestPasswordResetRequest request, [FromHeader(Name = "X-Environment")] string? environment = null)
    {
        var result = await _mediator.Send(new RequestPasswordResetCommand(request, environment), HttpContext.RequestAborted);
        if (!result.Success && result.ErrorCode == "NotFound")
            return Ok(result); // Don't leak existence; portal may expect 200 with success: false
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>Reset password with token. Body: { token, newPassword }.</summary>
    [HttpPost("resetPassword")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _mediator.Send(new ResetPasswordCommand(request), HttpContext.RequestAborted);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>Verify email with code. Body: { code }.</summary>
    [HttpPost("verify")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> Verify([FromBody] VerifyRequest request)
    {
        var result = await _mediator.Send(new VerifyEmailCommand(request), HttpContext.RequestAborted);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>Update verification status. Body: { userID, verification_status }.</summary>
    [HttpPost("update-status")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResult>> UpdateVerificationStatus([FromBody] UpdateVerificationStatusRequest request)
    {
        var result = await _mediator.Send(new UpdateVerificationStatusCommand(request), HttpContext.RequestAborted);
        if (!result.Success && result.ErrorCode == "NotFound")
            return NotFound(result);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}

/// <summary>DTO for GET /api/v1/portal/branding (camelCase in JSON).</summary>
public class BrandingResponse
{
    public string AppName { get; set; } = "";
    public string LogoUrl { get; set; } = "";
    public string PrimaryColor { get; set; } = "";
    public string SecondaryColor { get; set; } = "";
}

/// <summary>DTO for GET /api/v1/portal/couriers.</summary>
public class CouriersResponse
{
    public IReadOnlyList<string> CourierIds { get; set; } = Array.Empty<string>();
}
