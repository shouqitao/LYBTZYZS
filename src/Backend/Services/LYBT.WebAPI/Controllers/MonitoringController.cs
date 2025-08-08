using Asp.Versioning;
using LYBT.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 系统监控 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Roles = "Admin")]
    public class MonitoringController : BaseController
    {
        private readonly ISystemMetricsCollector _metricsCollector;

        public MonitoringController(
            ISystemMetricsCollector metricsCollector,
            ILogger<MonitoringController> logger)
            : base(logger)
        {
            _metricsCollector = metricsCollector;
        }

        /// <summary>
        /// 获取API性能统计
        /// </summary>
        [HttpGet("api/performance")]
        public async Task<ActionResult<ApiPerformanceStats>> GetApiPerformanceStats()
        {
            try
            {
                var stats = await _metricsCollector.GetApiPerformanceStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取API性能统计");
            }
        }

        /// <summary>
        /// 获取错误统计
        /// </summary>
        [HttpGet("errors")]
        public async Task<ActionResult<ErrorStats>> GetErrorStats()
        {
            try
            {
                var errorStats = await _metricsCollector.GetErrorStatsAsync();
                return Ok(errorStats);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取错误统计");
            }
        }

        /// <summary>
        /// 获取热点API端点统计
        /// </summary>
        [HttpGet("api/hotspots")]
        public async Task<ActionResult<List<ApiEndpointStats>>> GetHotApiEndpoints([FromQuery] int count = 10)
        {
            try
            {
                if (count <= 0 || count > 50)
                {
                    return BadRequest("count参数必须在1-50之间");
                }

                var hotspots = await _metricsCollector.GetHotApiEndpointsAsync(count);
                return Ok(hotspots);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取热点API端点");
            }
        }

        /// <summary>
        /// 获取性能趋势数据
        /// </summary>
        [HttpGet("performance/trend")]
        public async Task<ActionResult<SystemPerformanceTrend>> GetPerformanceTrend([FromQuery] string period = "1h")
        {
            try
            {
                var timeSpan = ParsePeriod(period);
                if (timeSpan == null)
                {
                    return BadRequest("无效的时间段格式，支持格式：1m, 5m, 15m, 30m, 1h, 6h, 12h, 24h");
                }

                var trend = await _metricsCollector.GetPerformanceTrendAsync(timeSpan.Value);
                return Ok(trend);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取性能趋势");
            }
        }

        /// <summary>
        /// 获取系统监控仪表板数据
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<MonitoringDashboardData>> GetDashboardData()
        {
            try
            {
                // 并行收集所有监控数据
                var apiPerformanceTask = _metricsCollector.GetApiPerformanceStatsAsync();
                var errorStatsTask = _metricsCollector.GetErrorStatsAsync();
                var hotEndpointsTask = _metricsCollector.GetHotApiEndpointsAsync(5);
                var performanceTrendTask = _metricsCollector.GetPerformanceTrendAsync(TimeSpan.FromHours(1));

                await Task.WhenAll(apiPerformanceTask, errorStatsTask, hotEndpointsTask, performanceTrendTask);

                var dashboardData = new MonitoringDashboardData
                {
                    ApiPerformance = await apiPerformanceTask,
                    ErrorStats = await errorStatsTask,
                    HotEndpoints = await hotEndpointsTask,
                    PerformanceTrend = await performanceTrendTask,
                    LastUpdated = DateTime.UtcNow
                };

                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取监控仪表板数据");
            }
        }

        /// <summary>
        /// 清理过期的监控数据
        /// </summary>
        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupExpiredData()
        {
            try
            {
                await _metricsCollector.CleanupExpiredMetricsAsync();
                LogOperation("清理过期监控数据", null, null);
                return Ok(new { message = "过期监控数据清理完成", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "清理过期监控数据");
            }
        }

        /// <summary>
        /// 获取监控配置信息
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetMonitoringConfig()
        {
            try
            {
                var config = new MonitoringConfigInfo
                {
                    MetricsRetentionHours = 24,
                    MaxMetricsInMemory = 10000,
                    SnapshotIntervalMinutes = 1,
                    CleanupIntervalHours = 1,
                    PerformanceThresholds = new PerformanceThresholds
                    {
                        SlowRequestMs = 2000,
                        VerySlowRequestMs = 5000,
                        HighCpuPercent = 70,
                        CriticalCpuPercent = 90,
                        HighMemoryPercent = 70,
                        CriticalMemoryPercent = 90
                    },
                    EnabledFeatures = new List<string>
                    {
                        "ApiPerformanceTracking",
                        "ErrorTracking",
                        "SystemResourceMonitoring",
                        "HealthChecks",
                        "PerformanceTrends"
                    }
                };

                return Ok(config);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取监控配置");
            }
        }

        /// <summary>
        /// 获取实时监控状态
        /// </summary>
        [HttpGet("status/realtime")]
        public async Task<ActionResult<RealtimeMonitoringStatus>> GetRealtimeStatus()
        {
            try
            {
                var apiStats = await _metricsCollector.GetApiPerformanceStatsAsync();
                var errorStats = await _metricsCollector.GetErrorStatsAsync();

                var status = new RealtimeMonitoringStatus
                {
                    IsHealthy = errorStats.ErrorRate < 0.05, // 错误率低于5%认为健康
                    RequestsPerMinute = apiStats.RequestsPerMinute,
                    AverageResponseTimeMs = apiStats.AverageResponseTime.TotalMilliseconds,
                    ErrorRate = errorStats.ErrorRate,
                    SuccessRate = apiStats.SuccessRate,
                    ActiveAlertsCount = CalculateActiveAlerts(apiStats, errorStats),
                    LastUpdated = DateTime.UtcNow,
                    
                    StatusLevel = DetermineStatusLevel(apiStats, errorStats)
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取实时监控状态");
            }
        }

        /// <summary>
        /// 获取告警列表
        /// </summary>
        [HttpGet("alerts")]
        public async Task<ActionResult<List<MonitoringAlert>>> GetActiveAlerts()
        {
            try
            {
                var alerts = new List<MonitoringAlert>();
                
                var apiStats = await _metricsCollector.GetApiPerformanceStatsAsync();
                var errorStats = await _metricsCollector.GetErrorStatsAsync();

                // 检查响应时间告警
                if (apiStats.AverageResponseTime.TotalMilliseconds > 2000)
                {
                    alerts.Add(new MonitoringAlert
                    {
                        Id = "slow_response_time",
                        Level = apiStats.AverageResponseTime.TotalMilliseconds > 5000 ? AlertLevel.Critical : AlertLevel.Warning,
                        Title = "响应时间过慢",
                        Description = $"平均响应时间: {apiStats.AverageResponseTime.TotalMilliseconds:F0}ms",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-5), // 模拟创建时间
                        Category = "Performance"
                    });
                }

                // 检查错误率告警
                if (errorStats.ErrorRate > 0.05)
                {
                    alerts.Add(new MonitoringAlert
                    {
                        Id = "high_error_rate",
                        Level = errorStats.ErrorRate > 0.1 ? AlertLevel.Critical : AlertLevel.Warning,
                        Title = "错误率过高",
                        Description = $"当前错误率: {errorStats.ErrorRate:P2}",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-3),
                        Category = "Reliability"
                    });
                }

                // 检查请求量异常
                if (apiStats.RequestsPerMinute > 1000)
                {
                    alerts.Add(new MonitoringAlert
                    {
                        Id = "high_request_volume",
                        Level = AlertLevel.Info,
                        Title = "请求量较高",
                        Description = $"当前请求量: {apiStats.RequestsPerMinute:F0}/分钟",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                        Category = "Traffic"
                    });
                }

                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取告警列表");
            }
        }

        private static TimeSpan? ParsePeriod(string period)
        {
            return period.ToLower() switch
            {
                "1m" => TimeSpan.FromMinutes(1),
                "5m" => TimeSpan.FromMinutes(5),
                "15m" => TimeSpan.FromMinutes(15),
                "30m" => TimeSpan.FromMinutes(30),
                "1h" => TimeSpan.FromHours(1),
                "6h" => TimeSpan.FromHours(6),
                "12h" => TimeSpan.FromHours(12),
                "24h" => TimeSpan.FromHours(24),
                _ => null
            };
        }

        private static int CalculateActiveAlerts(ApiPerformanceStats apiStats, ErrorStats errorStats)
        {
            var alertCount = 0;

            if (apiStats.AverageResponseTime.TotalMilliseconds > 2000) alertCount++;
            if (errorStats.ErrorRate > 0.05) alertCount++;
            if (apiStats.RequestsPerMinute > 1000) alertCount++;

            return alertCount;
        }

        private static string DetermineStatusLevel(ApiPerformanceStats apiStats, ErrorStats errorStats)
        {
            if (errorStats.ErrorRate > 0.1 || apiStats.AverageResponseTime.TotalMilliseconds > 5000)
                return "Critical";

            if (errorStats.ErrorRate > 0.05 || apiStats.AverageResponseTime.TotalMilliseconds > 2000)
                return "Warning";

            return "Healthy";
        }
    }

    // 数据传输对象
    public class MonitoringDashboardData
    {
        public ApiPerformanceStats ApiPerformance { get; set; } = new();
        public ErrorStats ErrorStats { get; set; } = new();
        public List<ApiEndpointStats> HotEndpoints { get; set; } = new();
        public SystemPerformanceTrend PerformanceTrend { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class MonitoringConfigInfo
    {
        public int MetricsRetentionHours { get; set; }
        public int MaxMetricsInMemory { get; set; }
        public int SnapshotIntervalMinutes { get; set; }
        public int CleanupIntervalHours { get; set; }
        public PerformanceThresholds PerformanceThresholds { get; set; } = new();
        public List<string> EnabledFeatures { get; set; } = new();
    }

    public class PerformanceThresholds
    {
        public int SlowRequestMs { get; set; }
        public int VerySlowRequestMs { get; set; }
        public int HighCpuPercent { get; set; }
        public int CriticalCpuPercent { get; set; }
        public int HighMemoryPercent { get; set; }
        public int CriticalMemoryPercent { get; set; }
    }

    public class RealtimeMonitoringStatus
    {
        public bool IsHealthy { get; set; }
        public double RequestsPerMinute { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public double ErrorRate { get; set; }
        public double SuccessRate { get; set; }
        public int ActiveAlertsCount { get; set; }
        public DateTime LastUpdated { get; set; }
        public string StatusLevel { get; set; } = string.Empty;
    }

    public class MonitoringAlert
    {
        public string Id { get; set; } = string.Empty;
        public AlertLevel Level { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public enum AlertLevel
    {
        Info,
        Warning,
        Critical
    }
}