using System.Reflection;
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
/// 架构修复：使用 IHealthCheckService 替代直接 DbContext 依赖
/// P2-12-5 评估：Health（/health探针/详情）与 Diagnostics（/diagnostics/logging-level）职责分离，前者探活后者运维，保留分散不合并。
/// </summary>
/// <remarks>
/// 内部健康检查端点 (需认证)。外部监控请使用中间件端点 GET /health (匿名)。
/// </remarks>
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
    [ProducesResponseType(typeof(ApiResponse<HealthStatusDto>), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return BuildHealthStatus("Healthy");
    }

    /// <summary>
    /// Ping端点
    /// </summary>
    /// <returns>Pong响应</returns>
    [HttpGet("ping")]
    [AllowAnonymous]  // Ping端点允许匿名访问
    [ProducesResponseType(typeof(ApiResponse<HealthStatusDto>), StatusCodes.Status200OK)]
    public IActionResult Ping()
    {
        return BuildHealthStatus("Pong");
    }

    /// <summary>
    /// 构建健康状态响应
    /// </summary>
    private IActionResult BuildHealthStatus(string status)
    {
        return Success(new HealthStatusDto
        {
            Status = status,
            Timestamp = DateTime.UtcNow,
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        });
    }

    /// <summary>
    /// 详细健康检查端点 - 包含数据库连接检查
    /// </summary>
    /// <returns>详细的系统健康状态</returns>
    [HttpGet("details")]
    [Authorize]  // 详细健康检查需要认证
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDetailedHealth(CancellationToken ct = default)
    {
        // Architecture Fix: 使用IHealthCheckService执行健康检查
        var dbCheck = await _healthCheckService.CheckDatabaseAsync(ct);

        var overallStatus = dbCheck.Status;
        var statusString = overallStatus switch
        {
            HealthStatus.Healthy => "Healthy",
            HealthStatus.Degraded => "Degraded",
            HealthStatus.Unhealthy => "Unhealthy",
            _ => "Unknown"
        };

        var response = new HealthStatusDto
        {
            Status = statusString,
            Timestamp = DateTime.UtcNow,
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        };

        // P2-8: 503 时 body 的 success 必须为 false（原恒 CreateSuccess 致探针误判）
        var isHealthy = overallStatus == HealthStatus.Healthy;
        var statusCode = isHealthy ? 200 : 503;
        var body = new ApiResponse<object>
        {
            Success = isHealthy,
            Message = isHealthy ? "操作成功" : $"健康检查失败：{statusString}",
            Data = response
        };
        return StatusCode(statusCode, body);
    }
}


