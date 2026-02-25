using Asp.Versioning;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 健康检查控制器 - MVP简化版（Issue #1733 Task 1.1）
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/health")]
[Authorize]  // 默认需要认证，公开端点使用 AllowAnonymous 覆盖
public class HealthController : BaseApiController
{
    private readonly AppDbContext _dbContext;

    public HealthController(AppDbContext dbContext, ILogger<HealthController> logger)
        : base(logger)
    {
        _dbContext = dbContext;
    }
    /// <summary>
    /// 基础健康检查 - 快速探活端点
    /// </summary>
    /// <returns>健康状态</returns>
    [HttpGet]
    [AllowAnonymous]  // 基础健康检查允许匿名访问
    public IActionResult Get()
    {
        return Ok(new
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
        return Ok(new
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
        // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
        // 执行数据库连接检查
        var dbCheck = await CheckDatabase();

        var overallStatus = dbCheck.Status; // 直接使用 CheckDatabase 返回的准确状态 (Healthy/Degraded/Unhealthy)

        var response = new
        {
            status = overallStatus,
            timestamp = DateTime.UtcNow,
            database = new
            {
                status = dbCheck.Status,
                duration = dbCheck.Duration,
                provider = dbCheck.Provider,
                pendingMigrations = dbCheck.PendingMigrationCount,
                serverVersion = dbCheck.ServerVersion
            }
        };

        var statusCode = overallStatus == "Healthy" ? 200 : 503;
        return StatusCode(statusCode, response);
    }

    private async Task<HealthCheck> CheckDatabase()
    {
        var check = new HealthCheck("db", "Database Connectivity");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            if (!canConnect)
            {
                check.Status = "Unhealthy";
                return check;
            }

            // 获取数据库 Provider 名称
            check.Provider = _dbContext.Database.ProviderName;

            // 检查是否为关系型数据库（排除 InMemory 数据库）
            var isRelationalDatabase = _dbContext.Database.IsRelational();

            if (isRelationalDatabase)
            {
                // 获取服务器版本信息
                try
                {
                    var connection = _dbContext.Database.GetDbConnection();
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        check.ServerVersion = connection.ServerVersion;
                    }
                }
                catch
                {
                    // ServerVersion 获取失败不影响健康检查
                }

                // 仅在关系型数据库上检查迁移
                var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
                var pendingCount = pendingMigrations.Count();
                check.PendingMigrationCount = pendingCount;

                check.Status = pendingCount == 0 ? "Healthy" : "Degraded";
            }
            else
            {
                // InMemory 或其他非关系型数据库
                check.Status = "Healthy";
            }
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            _logger.LogError(ex, "Database health check failed");
        }
        finally
        {
            stopwatch.Stop();
            check.Duration = stopwatch.ElapsedMilliseconds;
        }

        return check;
    }

    private class HealthCheck
    {
        public HealthCheck(string name, string description)
        {
            Name = name;
            Status = "Unknown";
        }

        public string Name { get; }
        public string Status { get; set; } = string.Empty;
        public long Duration { get; set; }
        public string? Provider { get; set; }
        public int PendingMigrationCount { get; set; }
        public string? ServerVersion { get; set; }
    }
}
