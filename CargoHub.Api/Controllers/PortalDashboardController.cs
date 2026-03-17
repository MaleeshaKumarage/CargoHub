using System.Security.Claims;
using CargoHub.Application.Auth;
using CargoHub.Application.Bookings.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CargoHub.Api.Controllers;

[ApiController]
[Route("api/v1/portal/dashboard")]
[Authorize]
public class PortalDashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public PortalDashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Get booking stats. Super Admin sees all users' bookings; other users see only their own.</summary>
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        var isSuperAdmin = User.IsInRole(RoleNames.SuperAdmin);
        string? customerId = null;
        if (!isSuperAdmin)
        {
            customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(customerId))
                return Unauthorized();
        }
        var stats = await _mediator.Send(new GetDashboardStatsQuery(customerId), HttpContext.RequestAborted);
        return Ok(stats);
    }
}
