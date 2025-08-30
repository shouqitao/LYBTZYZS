using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Performance;
using LYBT.Infrastructure.Web;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 性能监控控制器 - UltraThink重构性能优化架构
    /// 提供系统性能数据的REST API接口
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize] // 性能数据需要身份验证
    public class PerformanceController : BaseSystemController
    {
        private readonly CQRSPerformanceMonitor _cqrsMonitor;
        private readonly IPerformanceCollector _performanceCollector;
        private readonly ILogger<PerformanceController> _logger;

        public PerformanceController(
            CQRSPerformanceMonitor cqrsMonitor,
            IPerformanceCollector performanceCollector,
            ILogger<PerformanceController> logger)
            : base(logger) // 传递给BaseSystemController
        {
            _cqrsMonitor = cqrsMonitor;
            _performanceCollector = performanceCollector;
            _logger = logger;
        }

        /// <summary>
        /// 获取CQRS性能报告
        /// </summary>
        [HttpGet("cqrs/report")]
        public async Task<ActionResult<CQRSPerformanceReport>> GetCQRSReport()
        {
            try
            {
                var report = _cqrsMonitor.GetPerformanceReport();
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CQRS performance report");
                return StatusCode(500, new { message = "获取CQRS性能报告失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取特定操作的性能统计
        /// </summary>
        [HttpGet("cqrs/operations/{operationType}/{operationName}")]
        public async Task<ActionResult<CQRSOperationStats>> GetOperationStats(
            string operationType, 
            string operationName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(operationType) || string.IsNullOrWhiteSpace(operationName))
                {
                    return BadRequest(new { message = "操作类型和操作名称不能为空" });
                }

                var stats = _cqrsMonitor.GetOperationStats(operationType, operationName);
                
                if (stats == null)
                {
                    return NotFound(new { message = $"未找到操作 {operationType}.{operationName} 的统计数据" });
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting operation stats for {OperationType}.{OperationName}", 
                    operationType, operationName);
                return StatusCode(500, new { message = "获取操作统计数据失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取所有操作的性能统计概览
        /// </summary>
        [HttpGet("cqrs/operations")]
        public async Task<ActionResult<Dictionary<string, CQRSOperationStats>>> GetAllOperationsStats()
        {
            try
            {
                var allStats = _cqrsMonitor.GetOperationStats();
                return Ok(allStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all operations stats");
                return StatusCode(500, new { message = "获取所有操作统计数据失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取系统性能快照
        /// </summary>
        [HttpGet("system/snapshot")]
        public async Task<ActionResult<SystemPerformanceInfo>> GetSystemSnapshot()
        {
            try
            {
                using var monitor = new SystemPerformanceMonitor();
                var snapshot = monitor.GetCurrentInfo();
                return Ok(snapshot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system performance snapshot");
                return StatusCode(500, new { message = "获取系统性能快照失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取性能指标（如果使用内存收集器）
        /// </summary>
        [HttpGet("metrics")]
        public async Task<ActionResult<List<PerformanceMetric>>> GetMetrics(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int limit = 1000)
        {
            try
            {
                if (_performanceCollector is InMemoryPerformanceCollector memoryCollector)
                {
                    List<PerformanceMetric> metrics;
                    
                    if (from.HasValue && to.HasValue)
                    {
                        metrics = memoryCollector.GetMetrics(from.Value, to.Value);
                    }
                    else
                    {
                        metrics = memoryCollector.GetMetrics();
                    }

                    // 限制返回数量
                    if (metrics.Count > limit)
                    {
                        metrics = metrics.OrderByDescending(m => m.Timestamp).Take(limit).ToList();
                    }

                    return Ok(metrics);
                }
                else
                {
                    return BadRequest(new { message = "当前性能收集器不支持历史指标查询" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics");
                return StatusCode(500, new { message = "获取性能指标失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取慢操作列表
        /// </summary>
        [HttpGet("slow-operations")]
        public async Task<ActionResult<List<CQRSOperationStats>>> GetSlowOperations(
            [FromQuery] double thresholdMs = 1000)
        {
            try
            {
                var allStats = _cqrsMonitor.GetOperationStats();
                var slowOperations = allStats.Values
                    .Where(s => s.AverageExecutionTimeMs > thresholdMs)
                    .OrderByDescending(s => s.AverageExecutionTimeMs)
                    .ToList();

                return Ok(slowOperations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting slow operations");
                return StatusCode(500, new { message = "获取慢操作列表失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取错误率高的操作列表
        /// </summary>
        [HttpGet("error-prone-operations")]
        public async Task<ActionResult<List<CQRSOperationStats>>> GetErrorProneOperations(
            [FromQuery] double maxSuccessRate = 0.95)
        {
            try
            {
                var allStats = _cqrsMonitor.GetOperationStats();
                var errorProneOperations = allStats.Values
                    .Where(s => s.ExecutionCount >= 10 && s.SuccessRate < maxSuccessRate) // 至少执行10次且成功率低于阈值
                    .OrderBy(s => s.SuccessRate)
                    .ToList();

                return Ok(errorProneOperations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting error-prone operations");
                return StatusCode(500, new { message = "获取高错误率操作列表失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取性能趋势数据
        /// </summary>
        [HttpGet("trends")]
        public async Task<ActionResult<PerformanceTrends>> GetPerformanceTrends(
            [FromQuery] int durationMinutes = 60)
        {
            try
            {
                if (_performanceCollector is InMemoryPerformanceCollector memoryCollector)
                {
                    var endTime = DateTime.UtcNow;
                    var startTime = endTime.AddMinutes(-durationMinutes);
                    
                    var metrics = memoryCollector.GetMetrics(startTime, endTime);
                    
                    var trends = new PerformanceTrends
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        DurationMinutes = durationMinutes,
                        TotalMetrics = metrics.Count
                    };

                    // 计算命令和查询的趋势
                    var commandMetrics = metrics
                        .Where(m => m.Tags.ContainsKey("operation_type") && 
                                   m.Tags["operation_type"].ToString() == "Command")
                        .ToList();
                    
                    var queryMetrics = metrics
                        .Where(m => m.Tags.ContainsKey("operation_type") && 
                                   m.Tags["operation_type"].ToString() == "Query")
                        .ToList();

                    if (commandMetrics.Any())
                    {
                        trends.CommandTrends = new OperationTrends
                        {
                            TotalCount = commandMetrics.Count,
                            AverageResponseTime = commandMetrics.Where(m => m.Unit == "ms").Average(m => m.Value),
                            MaxResponseTime = commandMetrics.Where(m => m.Unit == "ms").Max(m => m.Value),
                            MinResponseTime = commandMetrics.Where(m => m.Unit == "ms").Min(m => m.Value)
                        };
                    }

                    if (queryMetrics.Any())
                    {
                        trends.QueryTrends = new OperationTrends
                        {
                            TotalCount = queryMetrics.Count,
                            AverageResponseTime = queryMetrics.Where(m => m.Unit == "ms").Average(m => m.Value),
                            MaxResponseTime = queryMetrics.Where(m => m.Unit == "ms").Max(m => m.Value),
                            MinResponseTime = queryMetrics.Where(m => m.Unit == "ms").Min(m => m.Value)
                        };
                    }

                    return Ok(trends);
                }
                else
                {
                    return BadRequest(new { message = "当前性能收集器不支持趋势数据查询" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance trends");
                return StatusCode(500, new { message = "获取性能趋势数据失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取性能优化建议
        /// </summary>
        [HttpGet("optimization-report")]
        [Authorize(Roles = "Admin")] // 仅管理员可查看优化建议
        public async Task<ActionResult<PerformanceOptimizationReport>> GetOptimizationReport()
        {
            try
            {
                // 注入性能优化引擎
                var optimizationEngine = HttpContext.RequestServices.GetService<PerformanceOptimizationEngine>();
                
                if (optimizationEngine == null)
                {
                    return BadRequest(new { message = "性能优化引擎未配置" });
                }

                var report = await optimizationEngine.GenerateOptimizationReportAsync();
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating performance optimization report");
                return StatusCode(500, new { message = "生成性能优化报告失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取性能健康检查
        /// </summary>
        [HttpGet("health-check")]
        public async Task<ActionResult<object>> GetHealthCheck()
        {
            try
            {
                var cqrsReport = _cqrsMonitor.GetPerformanceReport();
                
                using var systemMonitor = new SystemPerformanceMonitor();
                var systemInfo = systemMonitor.GetCurrentInfo();

                var healthStatus = new
                {
                    timestamp = DateTime.UtcNow,
                    overall_status = GetSimpleHealthStatus(cqrsReport, systemInfo),
                    cqrs_metrics = new
                    {
                        total_operations = cqrsReport.TotalOperations,
                        average_response_time = cqrsReport.AverageResponseTime,
                        success_rate = cqrsReport.OverallSuccessRate,
                        slow_operations_count = cqrsReport.SlowOperations.Count,
                        error_operations_count = cqrsReport.ErrorProneOperations.Count
                    },
                    system_metrics = new
                    {
                        cpu_usage = systemInfo.CpuUsagePercent,
                        memory_usage_mb = systemInfo.MemoryUsedBytes / (1024.0 * 1024.0),
                        thread_count = systemInfo.ThreadCount,
                        gc_collections = systemInfo.GcGen0Collections + systemInfo.GcGen1Collections + systemInfo.GcGen2Collections
                    }
                };

                return Ok(healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health check");
                return StatusCode(500, new { message = "获取健康检查失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 清理性能数据（仅限管理员）
        /// </summary>
        [HttpDelete("metrics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ClearMetrics()
        {
            try
            {
                if (_performanceCollector is InMemoryPerformanceCollector memoryCollector)
                {
                    memoryCollector.Clear();
                    _logger.LogInformation("Performance metrics cleared by user {UserId}", HttpContext.User?.Identity?.Name);
                    return Ok(new { message = "性能指标数据已清理" });
                }
                else
                {
                    return BadRequest(new { message = "当前性能收集器不支持数据清理" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing performance metrics");
                return StatusCode(500, new { message = "清理性能指标失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取简单健康状态
        /// </summary>
        private string GetSimpleHealthStatus(CQRSPerformanceReport cqrsReport, SystemPerformanceInfo systemInfo)
        {
            if (cqrsReport.AverageResponseTime > 2000 || systemInfo.CpuUsagePercent > 90 || 
                cqrsReport.OverallSuccessRate < 0.9)
            {
                return "Critical";
            }
            
            if (cqrsReport.AverageResponseTime > 1000 || systemInfo.CpuUsagePercent > 80 || 
                cqrsReport.OverallSuccessRate < 0.95)
            {
                return "Warning";
            }
            
            return "Healthy";
        }
    }

    /// <summary>
    /// 性能趋势数据
    /// </summary>
    public class PerformanceTrends
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalMetrics { get; set; }
        public OperationTrends CommandTrends { get; set; }
        public OperationTrends QueryTrends { get; set; }
    }

    /// <summary>
    /// 操作趋势数据
    /// </summary>
    public class OperationTrends
    {
        public int TotalCount { get; set; }
        public double AverageResponseTime { get; set; }
        public double MaxResponseTime { get; set; }
        public double MinResponseTime { get; set; }
    }
}