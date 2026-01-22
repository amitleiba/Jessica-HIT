using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(ILogger<HealthController> logger) : ControllerBase
{
    private readonly ILogger<HealthController> _logger = logger;


    /// <summary>
    /// Health check endpoint to verify the gateway is running
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetHealth()
    {
        _logger.LogDebug("Health check endpoint hit");
        return Ok(new
        {
            Status = "Healthy",
            Service = "Gateway",
            Timestamp = DateTime.UtcNow
        });
    }
}
