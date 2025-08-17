using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 健康检查 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [AllowAnonymous] // 健康检查端点允许匿名访问
    public class HealthController : BaseSystemController
    {
        private readonly ISystemHealthService _healthService;

        public HealthController(
            ISystemHealthService healthService,
            ILogger<HealthController> logger)
            : base(logger)
        {
            _healthService = healthService;
        }

        /// <summary>
        /// 快速健康检查 - 用于负载均衡器
        /// </summary>
        [HttpGet]
        [HttpHead]
        public async Task<IActionResult> Get()
        {
            try
            {
                var health = await _healthService.GetOverallHealthAsync();
                
                return health.Status switch
                {
                    HealthStatus.Healthy => Ok(new { status = "healthy", timestamp = health.CheckedAt }),
                    HealthStatus.Degraded => Ok(new { status = "degraded", timestamp = health.CheckedAt }),
                    HealthStatus.Unhealthy => StatusCode(503, new { status = "unhealthy", timestamp = health.CheckedAt }),
                    _ => StatusCode(500, new { status = "unknown", timestamp = health.CheckedAt })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "健康检查失败");
                return StatusCode(503, new { status = "unhealthy", error = "health check failed", timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>
        /// 详细健康状态
        /// </summary>
        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailed()
        {
            try
            {
                var health = await _healthService.GetOverallHealthAsync();
                return Ok(health);
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "获取详细健康状态");
            }
        }

        /// <summary>
        /// 数据库健康检查
        /// </summary>
        [HttpGet("database")]
        public async Task<IActionResult> GetDatabaseHealth()
        {
            try
            {
                var dbHealth = await _healthService.GetDatabaseHealthAsync();
                
                var statusCode = dbHealth.Status switch
                {
                    HealthStatus.Healthy => 200,
                    HealthStatus.Degraded => 200,
                    HealthStatus.Unhealthy => 503,
                    _ => 500
                };

                return StatusCode(statusCode, dbHealth);
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "数据库健康检查");
            }
        }

        /// <summary>
        /// 系统资源状态
        /// </summary>
        [HttpGet("resources")]
        public async Task<IActionResult> GetSystemResources()
        {
            try
            {
                var resources = await _healthService.GetSystemResourcesAsync();
                return Ok(resources);
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "获取系统资源状态");
            }
        }

        /// <summary>
        /// 应用程序指标
        /// </summary>
        [HttpGet("metrics")]
        public async Task<IActionResult> GetApplicationMetrics()
        {
            try
            {
                var metrics = await _healthService.GetApplicationMetricsAsync();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "获取应用程序指标");
            }
        }

        /// <summary>
        /// 完整健康报告
        /// </summary>
        [HttpGet("report")]
        public async Task<IActionResult> GetHealthReport()
        {
            try
            {
                var report = await _healthService.GetDetailedHealthReportAsync();
                return Ok(report);
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "获取健康报告");
            }
        }

        /// <summary>
        /// 就绪状态检查 - Kubernetes readiness probe
        /// </summary>
        [HttpGet("ready")]
        public async Task<IActionResult> GetReadiness()
        {
            try
            {
                // 检查关键组件是否就绪
                var dbHealth = await _healthService.GetDatabaseHealthAsync();
                
                if (dbHealth.Status == HealthStatus.Unhealthy)
                {
                    return StatusCode(503, new 
                    { 
                        status = "not ready", 
                        reason = "database unavailable",
                        timestamp = DateTime.UtcNow 
                    });
                }

                return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "就绪状态检查失败");
                return StatusCode(503, new 
                { 
                    status = "not ready", 
                    reason = "readiness check failed",
                    timestamp = DateTime.UtcNow 
                });
            }
        }

        /// <summary>
        /// 存活状态检查 - Kubernetes liveness probe
        /// </summary>
        [HttpGet("alive")]
        public IActionResult GetLiveness()
        {
            try
            {
                // 简单的存活检查 - 应用程序是否在运行
                return Ok(new 
                { 
                    status = "alive", 
                    uptime = GetApplicationUptime().ToString(),
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "存活状态检查失败");
                return StatusCode(500, new 
                { 
                    status = "not alive", 
                    reason = "liveness check failed",
                    timestamp = DateTime.UtcNow 
                });
            }
        }

        /// <summary>
        /// 启动状态检查 - Kubernetes startup probe
        /// </summary>
        [HttpGet("started")]
        public async Task<IActionResult> GetStartupStatus()
        {
            try
            {
                var uptime = GetApplicationUptime();
                
                // 如果应用程序运行时间少于30秒，检查是否完全启动
                if (uptime < TimeSpan.FromSeconds(30))
                {
                    var dbHealth = await _healthService.GetDatabaseHealthAsync();
                    if (dbHealth.Status == HealthStatus.Unhealthy)
                    {
                        return StatusCode(503, new 
                        { 
                            status = "starting", 
                            reason = "database not ready",
                            uptime = uptime.ToString(),
                            timestamp = DateTime.UtcNow 
                        });
                    }
                }

                return Ok(new 
                { 
                    status = "started", 
                    uptime = uptime.ToString(),
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动状态检查失败");
                return StatusCode(503, new 
                { 
                    status = "not started", 
                    reason = "startup check failed",
                    timestamp = DateTime.UtcNow 
                });
            }
        }

        /// <summary>
        /// 版本信息
        /// </summary>
        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version?.ToString() ?? "Unknown";
                var buildDate = GetBuildDate(assembly);
                
                return Ok(new
                {
                    version,
                    buildDate,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                    os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    machineName = Environment.MachineName,
                    uptime = GetApplicationUptime().ToString(),
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "获取版本信息");
            }
        }

        private static TimeSpan GetApplicationUptime()
        {
            return DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime;
        }

        private static DateTime GetBuildDate(System.Reflection.Assembly assembly)
        {
            try
            {
                var attribute = assembly.GetCustomAttribute<System.Reflection.AssemblyMetadataAttribute>();
                if (attribute?.Key == "BuildDate" && DateTime.TryParse(attribute.Value, out var buildDate))
                {
                    return buildDate;
                }
                
                // 作为备用方案，使用文件修改时间
                var location = assembly.Location;
                if (!string.IsNullOrEmpty(location) && System.IO.File.Exists(location))
                {
                    return System.IO.File.GetLastWriteTime(location);
                }
            }
            catch
            {
                // 忽略错误
            }

            return DateTime.MinValue;
        }
    }
}