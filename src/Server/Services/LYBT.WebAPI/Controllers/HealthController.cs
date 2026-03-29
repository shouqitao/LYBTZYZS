using Asp.Versioning;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 健康检查控制器 - 遵循三层架构
/// Architecture Fix: 使用IHealthCheckService替代直接DbContext依赖
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/health")]
[Authorize]  // 默认需要认证，公开端点使用 AllowAnonymous 覆盖
public class HealthController : BaseApiController
{
    private readonly IHealthCheckService _healthCheckService;

    public HealthController(IHealthCheckService healthCheckService, ILogger<HealthController> logger)
        : base(logger)
    {
        _healthCheckService = healthCheckService;
    }
    /// <summary>
    /// 基础健康检查 - 快速探活端点
    /// </summary>
    /// <returns>健康状态</returns>
    [HttpGet]
    [AllowAnonymous]  // 基础健康检查允许匿名访问
    public IActionResult Get()
    {
        return Success(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Ping端点
    /// </summary>
    /// <returns>Pong响应</returns>
    [HttpGet("ping")]
    [AllowAnonymous]  // Ping端点允许匿名访问
    public IActionResult Ping()
    {
        return Success(new
        {
            message = "pong",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 详细健康检查端点 - 包含数据库连接检查
    /// </summary>
    /// <returns>详细的系统健康状态</returns>
    [HttpGet("details")]
    [Authorize]  // 详细健康检查需要认证
    public async Task<IActionResult> GetDetailedHealth()
    {
        // Architecture Fix: 使用IHealthCheckService执行健康检查
        var dbCheck = await _healthCheckService.CheckDatabaseAsync();

        var overallStatus = dbCheck.Status;
        var statusString = overallStatus switch
        {
            HealthStatus.Healthy => "Healthy",
            HealthStatus.Degraded => "Degraded",
            HealthStatus.Unhealthy => "Unhealthy",
            _ => "Unknown"
        };

        var response = new
        {
            status = statusString,
            timestamp = DateTime.UtcNow,
            database = new
            {
                status = statusString,
                duration = dbCheck.Duration,
                provider = dbCheck.Provider,
                pendingMigrations = dbCheck.PendingMigrationCount,
                serverVersion = dbCheck.ServerVersion
            }
        };

        var statusCode = overallStatus == HealthStatus.Healthy ? 200 : 503;
        return StatusCode(statusCode, ApiResponse<object>.CreateSuccess(response));
    }
}
