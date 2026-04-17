using System.Net.Mime;
using System.Security.Claims;
using System.Text.Json;
using CargoHub.Application.Auth;
using CargoHub.Application.Company;
using CargoHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CargoHub.Api.Middleware;

/// <summary>
/// Blocks authenticated company users when their company is inactive (Super Admin bypass).
/// </summary>
public sealed class RequireActiveCompanyMiddleware
{
    private readonly RequestDelegate _next;

    public RequireActiveCompanyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        ICompanyRepository companies)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        if (context.User.IsInRole(RoleNames.SuperAdmin) || context.User.IsInRole(RoleNames.Rider))
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await _next(context);
            return;
        }

        var user = await userManager.FindByIdAsync(userId);
        var bid = user?.BusinessId?.Trim();
        if (string.IsNullOrEmpty(bid))
        {
            await _next(context);
            return;
        }

        var company = await companies.GetByBusinessIdAsync(bid, context.RequestAborted);
        if (company is { IsActive: false })
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = MediaTypeNames.Application.Json;
            var payload = JsonSerializer.Serialize(new
            {
                errorCode = "CompanyInactive",
                message = AuthMessages.CompanyInactive
            });
            await context.Response.WriteAsync(payload);
            return;
        }

        await _next(context);
    }
}
