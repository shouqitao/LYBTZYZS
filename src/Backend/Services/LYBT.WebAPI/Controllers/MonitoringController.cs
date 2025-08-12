using Asp.Versioning;
using LYBT.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 系统监控 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
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

        /// <summary>
        /// Prometheus健康检查端点 - UltraThink重构监控集成
        /// </summary>
        /// <returns>系统健康状态</returns>
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult GetHealth()
        {
            var healthCheck = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                Version = GetType().Assembly.GetName().Version?.ToString(),
                ServerName = Environment.MachineName,
                ProcessId = Environment.ProcessId,
                WorkingSet = GC.GetTotalMemory(false),
                GCCount = new
                {
                    Gen0 = GC.CollectionCount(0),
                    Gen1 = GC.CollectionCount(1),
                    Gen2 = GC.CollectionCount(2)
                },
                ThreadCount = Process.GetCurrentProcess().Threads.Count
            };

            return Ok(healthCheck);
        }

        /// <summary>
        /// Prometheus指标端点 - UltraThink重构监控集成
        /// </summary>
        /// <returns>Prometheus格式指标</returns>
        [HttpGet("metrics")]
        [AllowAnonymous]
        public IActionResult GetPrometheusMetrics()
        {
            var process = Process.GetCurrentProcess();
            var metrics = new[]
            {
                "# HELP lybt_process_cpu_seconds_total Total user and system CPU time spent in seconds",
                "# TYPE lybt_process_cpu_seconds_total counter",
                $"lybt_process_cpu_seconds_total {process.TotalProcessorTime.TotalSeconds}",
                
                "# HELP lybt_process_memory_bytes Current memory usage in bytes",
                "# TYPE lybt_process_memory_bytes gauge",
                $"lybt_process_memory_bytes {process.WorkingSet64}",
                
                "# HELP lybt_process_threads_total Current number of threads",
                "# TYPE lybt_process_threads_total gauge",
                $"lybt_process_threads_total {process.Threads.Count}",
                
                "# HELP lybt_gc_collections_total Number of garbage collections",
                "# TYPE lybt_gc_collections_total counter",
                $"lybt_gc_collections_total{{generation=\"0\"}} {GC.CollectionCount(0)}",
                $"lybt_gc_collections_total{{generation=\"1\"}} {GC.CollectionCount(1)}",
                $"lybt_gc_collections_total{{generation=\"2\"}} {GC.CollectionCount(2)}",
                
                "# HELP lybt_gc_memory_bytes Current managed memory usage",
                "# TYPE lybt_gc_memory_bytes gauge",
                $"lybt_gc_memory_bytes {GC.GetTotalMemory(false)}",
                
                "# HELP lybt_uptime_seconds Application uptime in seconds",
                "# TYPE lybt_uptime_seconds gauge",
                $"lybt_uptime_seconds {(DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds}"
            };

            return Content(string.Join("\n", metrics), "text/plain; version=0.0.4; charset=utf-8");
        }

        /// <summary>
        /// 接收AlertManager告警通知 - UltraThink重构监控集成
        /// </summary>
        /// <param name="webhook">告警数据</param>
        /// <returns>处理结果</returns>
        [HttpPost("webhooks/alertmanager")]
        [AllowAnonymous]
        public IActionResult ReceiveAlertManagerWebhook([FromBody] AlertManagerWebhook webhook)
        {
            try
            {
                Logger.LogInformation("收到AlertManager告警通知: {@Webhook}", webhook);

                // 处理告警逻辑
                foreach (var alert in webhook.Alerts)
                {
                    ProcessAlert(alert);
                }

                return Ok(new { Message = "告警处理成功", ProcessedCount = webhook.Alerts.Count });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "处理AlertManager告警时发生错误");
                return StatusCode(500, new { Error = "告警处理失败", Details = ex.Message });
            }
        }

        /// <summary>
        /// 接收严重告警通知
        /// </summary>
        /// <param name="webhook">严重告警数据</param>
        /// <returns>处理结果</returns>
        [HttpPost("webhooks/critical")]
        public IActionResult ReceiveCriticalAlert([FromBody] AlertManagerWebhook webhook)
        {
            try
            {
                Logger.LogCritical("收到严重告警通知: {@Webhook}", webhook);

                foreach (var alert in webhook.Alerts)
                {
                    ProcessCriticalAlert(alert);
                }

                return Ok(new { Message = "严重告警处理成功", ProcessedCount = webhook.Alerts.Count });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "处理严重告警时发生错误");
                return StatusCode(500, new { Error = "严重告警处理失败", Details = ex.Message });
            }
        }

        /// <summary>
        /// 接收安全告警通知
        /// </summary>
        /// <param name="webhook">安全告警数据</param>
        /// <returns>处理结果</returns>
        [HttpPost("webhooks/security")]
        public IActionResult ReceiveSecurityAlert([FromBody] AlertManagerWebhook webhook)
        {
            try
            {
                Logger.LogWarning("收到安全告警通知: {@Webhook}", webhook);

                foreach (var alert in webhook.Alerts)
                {
                    ProcessSecurityAlert(alert);
                }

                return Ok(new { Message = "安全告警处理成功", ProcessedCount = webhook.Alerts.Count });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "处理安全告警时发生错误");
                return StatusCode(500, new { Error = "安全告警处理失败", Details = ex.Message });
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

        /// <summary>
        /// 处理一般告警 - UltraThink重构监控集成
        /// </summary>
        private void ProcessAlert(PrometheusAlert alert)
        {
            Logger.LogWarning("处理告警: {AlertName}, 严重程度: {Severity}, 状态: {Status}",
                alert.Labels.GetValueOrDefault("alertname", "Unknown"),
                alert.Labels.GetValueOrDefault("severity", "Unknown"),
                alert.Status);

            // 可以添加更多的告警处理逻辑：
            // - 发送通知到企业微信
            // - 更新数据库记录
            // - 触发自动恢复机制
        }

        /// <summary>
        /// 处理严重告警
        /// </summary>
        private void ProcessCriticalAlert(PrometheusAlert alert)
        {
            Logger.LogCritical("严重告警处理: {AlertName}, 描述: {Description}",
                alert.Labels.GetValueOrDefault("alertname", "Unknown"),
                alert.Annotations.GetValueOrDefault("description", "无描述"));

            // 严重告警可能需要：
            // - 立即通知管理员
            // - 自动扩容
            // - 启用降级服务
        }

        /// <summary>
        /// 处理安全告警
        /// </summary>
        private void ProcessSecurityAlert(PrometheusAlert alert)
        {
            Logger.LogWarning("安全告警处理: {AlertName}, 服务: {Service}",
                alert.Labels.GetValueOrDefault("alertname", "Unknown"),
                alert.Labels.GetValueOrDefault("service", "Unknown"));

            // 安全告警可能需要：
            // - 记录安全日志
            // - 更新防火墙规则
            // - 通知安全团队
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

    /// <summary>
    /// AlertManager Webhook数据模型 - UltraThink重构监控集成
    /// </summary>
    public class AlertManagerWebhook
    {
        public string Receiver { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<PrometheusAlert> Alerts { get; set; } = new();
        public Dictionary<string, string> GroupLabels { get; set; } = new();
        public Dictionary<string, string> CommonLabels { get; set; } = new();
        public Dictionary<string, string> CommonAnnotations { get; set; } = new();
        public string ExternalURL { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string GroupKey { get; set; } = string.Empty;
        public int TruncatedAlerts { get; set; }
    }

    /// <summary>
    /// Prometheus告警数据模型
    /// </summary>
    public class PrometheusAlert
    {
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, string> Labels { get; set; } = new();
        public Dictionary<string, string> Annotations { get; set; } = new();
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public string GeneratorURL { get; set; } = string.Empty;
        public string Fingerprint { get; set; } = string.Empty;
    }
}