using System.Reflection;
using Asp.Versioning;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 健康检查控制器
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/health")]
[Authorize]  // 默认需要认证，公开端点使用 AllowAnonymous 覆盖
public class HealthController : BaseApiController
{
    private readonly AppDbContext _dbContext;
    private static readonly DateTime _startupTime = DateTime.UtcNow;

    private readonly IWebHostEnvironment _environment;

    public HealthController(AppDbContext dbContext, ILogger<HealthController> logger, IWebHostEnvironment environment, IMemoryCache? cache = null)
        : base(logger, cache)
    {
        _dbContext = dbContext;
        _environment = environment;
    }
    /// <summary>
    /// 基础健康检查
    /// </summary>
    /// <returns>健康状态</returns>
    [HttpGet]
    [AllowAnonymous]  // 基础健康检查允许匿名访问
    public IActionResult Get()
    {
        // 生产环境最小化信息暴露
        if (_environment.IsProduction())
        {
            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow
            });
        }

        // 开发环境可以返回更多信息
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
            environment = _environment.EnvironmentName
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
    /// 详细健康检查端点
    /// </summary>
    /// <returns>详细的系统健康状态</returns>
    [HttpGet("details")]
    [Authorize]  // 详细健康检查需要认证
    public async Task<IActionResult> GetDetailedHealth()
    {
        var startTime = DateTime.UtcNow;
        var checks = new List<HealthCheck>();
        var overallStatus = "Healthy";

        try
        {
            // 生产环境简化检查
            if (_environment.IsProduction())
            {
                // 仅执行关键检查
                var dbCheck = await CheckDatabase();
                checks.Add(new HealthCheck("system", "System Health")
                {
                    Status = dbCheck.Status,
                    Duration = dbCheck.Duration
                });

                if (dbCheck.Status != "Healthy") overallStatus = "Degraded";
            }
            else
            {
                // 开发环境执行全部检查
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
            }

            // 生产环境简化响应
            object response;
            if (_environment.IsProduction())
            {
                response = new
                {
                    status = overallStatus,
                    timestamp = DateTime.UtcNow,
                    checks = checks.Select(c => new
                    {
                        name = c.Name,
                        status = c.Status
                    }).ToArray()
                };
            }
            else
            {
                response = new
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
            }

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

            // 检查是否为关系型数据库（排除 InMemory 数据库）
            var isRelationalDatabase = _dbContext.Database.IsRelational();

            if (isRelationalDatabase)
            {
                // 仅在关系型数据库上检查迁移
                var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
                var pendingCount = pendingMigrations.Count();

                check.Status = pendingCount == 0 ? "Healthy" : "Degraded";
                check.Description = pendingCount == 0
                    ? "Database connection and migrations OK"
                    : $"{pendingCount} pending migrations";

                check.Data = new
                {
                    connected = true,
                    databaseType = "Relational",
                    pendingMigrations = pendingCount,
                    migrations = pendingMigrations.ToArray()
                };
            }
            else
            {
                // InMemory 或其他非关系型数据库
                check.Status = "Healthy";
                check.Description = "Database connection OK (non-relational)";
                check.Data = new
                {
                    connected = true,
                    databaseType = "InMemory"
                };
            }
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
            // 使用 EF Core LINQ 查询替代原始 SQL，更安全且类型安全
            // 注意：这里使用轻量级的 Any() 检查而非 Count()，性能更好
            var hasUsers = await _dbContext.Set<LYBT.Core.Entities.Users.User>().AnyAsync();
            var hasPatients = await _dbContext.Set<LYBT.Core.Entities.Patients.Patient>().AnyAsync();

            // 如果需要具体数量，可以选择性地获取（仅在数据存在时）
            var userCount = hasUsers ? await _dbContext.Set<LYBT.Core.Entities.Users.User>().CountAsync() : 0;
            var patientCount = hasPatients ? await _dbContext.Set<LYBT.Core.Entities.Patients.Patient>().CountAsync() : 0;

            check.Status = hasUsers ? "Healthy" : "Degraded";
            check.Description = hasUsers
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
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public object? Data { get; set; }
        public long Duration { get; set; }
        public string? Error { get; set; }
    }
}
