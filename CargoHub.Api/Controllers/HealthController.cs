using Microsoft.AspNetCore.Mvc;

namespace CargoHub.Api.Controllers;

/// <summary>
/// Health check for load balancers and monitoring. Scope: 12-Health-And-Swagger.
/// </summary>
[ApiController]
[Route("api/v1/health_")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// GET /api/v1/health_/ — returns 200 OK when the API is up.
    /// </summary>
    [HttpGet("")]
    public IActionResult Get()
    {
        return Ok(new { status = "OK", timestamp = DateTime.UtcNow });
    }
}
