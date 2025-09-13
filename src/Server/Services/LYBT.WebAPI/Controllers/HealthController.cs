using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 健康检查控制器
/// </summary>
[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// 基础健康检查
    /// </summary>
    /// <returns>健康状态</returns>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }

    /// <summary>
    /// Ping端点
    /// </summary>
    /// <returns>Pong响应</returns>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            message = "pong",
            timestamp = DateTime.UtcNow
        });
    }
}
