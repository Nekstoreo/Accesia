using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Accesia.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await _healthCheckService.CheckHealthAsync();
        
        _logger.LogInformation("Health check ejecutado: {Status}", result.Status);
        
        return result.Status == HealthStatus.Healthy 
            ? Ok(new { status = "Healthy", checks = result.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() }) })
            : StatusCode(503, new { status = result.Status.ToString(), checks = result.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), error = e.Value.Exception?.Message }) });
    }

    [HttpGet("simple")]
    public IActionResult Simple()
    {
        _logger.LogInformation("Health check simple ejecutado");
        return Ok(new { status = "API funcionando", timestamp = DateTime.UtcNow });
    }
} 