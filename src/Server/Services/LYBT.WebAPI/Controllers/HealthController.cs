using System.Reflection;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.Infrastructure.Data;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 健康检查控制器
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;
    private static readonly DateTime _startupTime = DateTime.UtcNow;

    public HealthController(AppDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
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

    /// <summary>
    /// 详细健康检查端点
    /// </summary>
    /// <returns>详细的系统健康状态</returns>
    [HttpGet("details")]
    public async Task<IActionResult> GetDetailedHealth()
    {
        var startTime = DateTime.UtcNow;
        var checks = new List<HealthCheck>();
        var overallStatus = "Healthy";

        try
        {
            // App信息检查
            checks.Add(await CheckAppInfo());

            // 数据库检查
            var dbCheck = await CheckDatabase();
            checks.Add(dbCheck);
            if (dbCheck.Status != "Healthy") overallStatus = "Degraded";

            // 外部依赖检查 (占位)
            checks.Add(CheckExternalDependencies());

            // 种子数据检查
            var seedCheck = await CheckSeedData();
            checks.Add(seedCheck);
            if (seedCheck.Status == "Unhealthy") overallStatus = "Unhealthy";

            var response = new
            {
                status = overallStatus,
                uptimeMs = (long)(DateTime.UtcNow - _startupTime).TotalMilliseconds,
                nowUtc = DateTime.UtcNow,
                checks = checks.Select(c => new
                {
                    name = c.Name,
                    status = c.Status,
                    description = c.Description,
                    data = c.Data,
                    duration = c.Duration,
                    error = c.Error
                }).ToArray()
            };

            var statusCode = overallStatus == "Unhealthy" ? 503 : 200;
            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detailed health check failed after {Duration}ms", 
                (DateTime.UtcNow - startTime).TotalMilliseconds);
            
            return StatusCode(503, new
            {
                status = "Unhealthy",
                uptimeMs = (long)(DateTime.UtcNow - _startupTime).TotalMilliseconds,
                nowUtc = DateTime.UtcNow,
                error = "Health check execution failed",
                checks = checks.ToArray()
            });
        }
    }

    private async Task<HealthCheck> CheckAppInfo()
    {
        var check = new HealthCheck("app", "Application Info");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await Task.CompletedTask; // 满足async约定
            
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
            
            check.Status = "Healthy";
            check.Data = new
            {
                version,
                environment,
                startupTime = _startupTime,
                runtime = Environment.Version.ToString()
            };
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            check.Error = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            check.Duration = stopwatch.ElapsedMilliseconds;
        }

        return check;
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
                check.Description = "Cannot connect to database";
                return check;
            }

            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
            var pendingCount = pendingMigrations.Count();

            check.Status = pendingCount == 0 ? "Healthy" : "Degraded";
            check.Description = pendingCount == 0 
                ? "Database connection and migrations OK" 
                : $"{pendingCount} pending migrations";
            
            check.Data = new
            {
                connected = true,
                pendingMigrations = pendingCount,
                migrations = pendingMigrations.ToArray()
            };
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            check.Error = ex.Message;
            _logger.LogError(ex, "Database health check failed");
        }
        finally
        {
            stopwatch.Stop();
            check.Duration = stopwatch.ElapsedMilliseconds;
        }

        return check;
    }

    private HealthCheck CheckExternalDependencies()
    {
        var check = new HealthCheck("deps", "External Dependencies");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 占位实现 - 未来可扩展缓存/消息队列检查
            check.Status = "Healthy";
            check.Description = "No external dependencies configured";
            check.Data = new
            {
                cache = new { status = "skipped", reason = "not_configured" },
                messageQueue = new { status = "skipped", reason = "not_configured" }
            };
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            check.Error = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            check.Duration = stopwatch.ElapsedMilliseconds;
        }

        return check;
    }

    private async Task<HealthCheck> CheckSeedData()
    {
        var check = new HealthCheck("seed", "Seed Data Verification");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var userCount = await _dbContext.Users.CountAsync();
            var patientCount = await _dbContext.Patients.CountAsync();
            
            check.Status = userCount > 0 ? "Healthy" : "Degraded";
            check.Description = userCount > 0 
                ? "Essential seed data present" 
                : "No users found - check seed data";
            
            check.Data = new
            {
                users = userCount,
                patients = patientCount
            };
        }
        catch (Exception ex)
        {
            check.Status = "Unhealthy";
            check.Error = ex.Message;
            _logger.LogError(ex, "Seed data check failed");
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
            Description = description;
            Status = "Unknown";
        }

        public string Name { get; }
        public string Status { get; set; }
        public string Description { get; set; }
        public object? Data { get; set; }
        public long Duration { get; set; }
        public string? Error { get; set; }
    }
}
