using System.Reflection;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthStatus = LYBT.Shared.Models.Contracts.Health.HealthStatus;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class HealthController : BaseApiController
{
    private readonly IHealthCheckService _healthCheckService;

    public HealthController(IHealthCheckService healthCheckService, ILogger<HealthController> logger)
        : base(logger)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var dbResult = await _healthCheckService.CheckDatabaseAsync();
        var status = dbResult.Status == HealthStatus.Healthy ? "Healthy" :
                     dbResult.Status == HealthStatus.Degraded ? "Degraded" : "Unhealthy";

        return Success(new HealthStatusDto
        {
            Status = status,
            Timestamp = DateTime.UtcNow,
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        });
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Success(new HealthStatusDto
        {
            Status = "Pong",
            Timestamp = DateTime.UtcNow,
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        });
    }

    [HttpGet("details")]
    [Authorize]
    public async Task<IActionResult> GetDetails()
    {
        var dbResult = await _healthCheckService.CheckDatabaseAsync();
        var status = dbResult.Status == HealthStatus.Healthy ? "Healthy" :
                     dbResult.Status == HealthStatus.Degraded ? "Degraded" : "Unhealthy";

        return Success(new HealthStatusDto
        {
            Status = status,
            Timestamp = DateTime.UtcNow,
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"
        });
    }
}
