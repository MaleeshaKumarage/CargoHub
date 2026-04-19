using System.Security.Claims;
using CargoHub.Application.Auth;
using CargoHub.Application.Company;
using CargoHub.Application.FreelanceRiders;
using CargoHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CargoHub.Api.Controllers;

/// <summary>Freelance rider matching for booking UI (not Super Admin management).</summary>
[ApiController]
[Route("api/v1/portal/freelance-riders")]
[Authorize]
public class PortalFreelanceRidersController : ControllerBase
{
    private readonly IFreelanceRiderRepository _riders;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICompanyRepository _companyRepository;

    public PortalFreelanceRidersController(
        IFreelanceRiderRepository riders,
        UserManager<ApplicationUser> userManager,
        ICompanyRepository companyRepository)
    {
        _riders = riders;
        _userManager = userManager;
        _companyRepository = companyRepository;
    }

    /// <summary>SuperAdmin may pass companyId to scope riders for that company; others use their own company.</summary>
    [HttpGet("matches")]
    public async Task<ActionResult<IReadOnlyList<object>>> GetMatches(
        [FromQuery] string shipperPostal,
        [FromQuery] string receiverPostal,
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        if (User.IsInRole(RoleNames.Rider))
            return Ok(Array.Empty<object>());

        Guid? bookingCompanyId;
        if (User.IsInRole(RoleNames.SuperAdmin))
            bookingCompanyId = companyId;
        else
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(uid)) return Unauthorized();
            var user = await _userManager.FindByIdAsync(uid);
            if (string.IsNullOrWhiteSpace(user?.BusinessId))
                bookingCompanyId = null;
            else
            {
                var company = await _companyRepository.GetByBusinessIdAsync(user.BusinessId, cancellationToken);
                bookingCompanyId = company?.Id;
            }
        }

        var list = await _riders.FindMatchingActiveAsync(shipperPostal, receiverPostal, bookingCompanyId, cancellationToken);
        var result = list.Select(r => new
        {
            id = r.Id,
            displayName = r.DisplayName,
            email = r.Email,
        }).ToList();
        return Ok(result);
    }
}
