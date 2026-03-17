using HiavaNet.Application.Auth.Commands;
using HiavaNet.Application.Auth.Dtos;
using HiavaNet.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace HiavaNet.Api.Controllers;

/// <summary>
/// Authentication endpoints for the portal.
/// These are designed to be compatible with the existing portal login / register flows.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(
        IMediator mediator,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _mediator = mediator;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    /// <summary>
    /// Registers a new portal user. Company ID (government business ID) must match an existing company.
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
    /// Authenticates an existing portal user.
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
}

