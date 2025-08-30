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
    /// 性能监控控制器 - UltraThink简化版本
    /// 提供系统性能数据的REST API接口，移除过度设计的CQRS监控
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize] // 性能数据需要身份验证
    public class PerformanceController : BaseSystemController
    {
        private readonly IPerformanceCollector _performanceCollector;
        // 移除过度设计的CQRS监控器：private readonly CQRSPerformanceMonitor _cqrsMonitor;

        public PerformanceController(
            IPerformanceCollector performanceCollector,
            ILogger<PerformanceController> logger)
            : base(logger) // 传递给BaseSystemController
        {
            _performanceCollector = performanceCollector;
        }

        /// <summary>
        /// 获取简化性能报告 - UltraThink简化版本
        /// </summary>
        [HttpGet("simple-report")]
        public async Task<ActionResult<object>> GetSimpleReport()
        {
            try
            {
                using var systemMonitor = new SystemPerformanceMonitor();
                var systemInfo = systemMonitor.GetCurrentInfo();

                var report = new
                {
                    timestamp = DateTime.UtcNow,
                    system_performance = new
                    {
                        cpu_usage_percent = systemInfo.CpuUsagePercent,
                        memory_used_mb = systemInfo.MemoryUsedBytes / (1024.0 * 1024.0),
                        thread_count = systemInfo.ThreadCount,
                        gc_collections = systemInfo.GcGen0Collections + systemInfo.GcGen1Collections + systemInfo.GcGen2Collections
                    },
                    status = systemInfo.CpuUsagePercent > 80 ? "Warning" : "Healthy"
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating simple performance report");
                return StatusCode(500, new { message = "获取性能报告失败", error = ex.Message });
            }
        }

        // UltraThink简化：移除复杂的CQRS操作统计，20人以下诊所不需要如此详细的监控

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

        // UltraThink简化：移除复杂的慢操作和错误操作统计，使用简单的系统监控即可

        // UltraThink简化：移除复杂的性能趋势分析，20人以下诊所不需要详细趋势统计

        /// <summary>
        /// 获取性能优化建议 - UltraThink简化版本
        /// </summary>
        [HttpGet("optimization-report")]
        [Authorize(Roles = "Admin")] // 仅管理员可查看优化建议
        public async Task<ActionResult<object>> GetOptimizationReport()
        {
            try
            {
                // UltraThink简化：提供基础性能建议，移除复杂的CQRS监控
                using var systemMonitor = new SystemPerformanceMonitor();
                var systemInfo = systemMonitor.GetCurrentInfo();

                var basicReport = new
                {
                    timestamp = DateTime.UtcNow,
                    system_status = GetSimpleHealthStatus(systemInfo),
                    recommendations = GetBasicRecommendations(systemInfo),
                    performance_summary = new
                    {
                        cpu_usage = systemInfo.CpuUsagePercent,
                        memory_usage_mb = systemInfo.MemoryUsedBytes / (1024.0 * 1024.0),
                        thread_count = systemInfo.ThreadCount,
                        gc_collections = systemInfo.GcGen0Collections + systemInfo.GcGen1Collections + systemInfo.GcGen2Collections
                    }
                };

                return Ok(basicReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating performance optimization report");
                return StatusCode(500, new { message = "生成性能优化报告失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取性能健康检查 - UltraThink简化版本
        /// </summary>
        [HttpGet("health-check")]
        public async Task<ActionResult<object>> GetHealthCheck()
        {
            try
            {
                // UltraThink简化：移除复杂的CQRS监控，只保留基础系统监控
                using var systemMonitor = new SystemPerformanceMonitor();
                var systemInfo = systemMonitor.GetCurrentInfo();

                var healthStatus = new
                {
                    timestamp = DateTime.UtcNow,
                    overall_status = GetSimpleHealthStatus(systemInfo),
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
        /// 获取简单健康状态 - UltraThink简化版本
        /// </summary>
        private string GetSimpleHealthStatus(SystemPerformanceInfo systemInfo)
        {
            // UltraThink简化：仅基于系统资源使用率判断健康状态
            if (systemInfo.CpuUsagePercent > 90)
            {
                return "Critical";
            }
            
            if (systemInfo.CpuUsagePercent > 80)
            {
                return "Warning";
            }
            
            var memoryUsageMB = systemInfo.MemoryUsedBytes / (1024.0 * 1024.0);
            if (memoryUsageMB > 2000) // 内存使用超过2GB为警告
            {
                return "Warning";
            }
            
            return "Healthy";
        }

        /// <summary>
        /// 获取基础性能建议 - UltraThink简化版本
        /// </summary>
        private List<string> GetBasicRecommendations(SystemPerformanceInfo systemInfo)
        {
            var recommendations = new List<string>();

            // CPU使用率建议
            if (systemInfo.CpuUsagePercent > 90)
            {
                recommendations.Add("CPU使用率过高(>90%)，建议检查CPU密集型操作");
            }
            else if (systemInfo.CpuUsagePercent > 80)
            {
                recommendations.Add("CPU使用率偏高(>80%)，建议监控系统负载");
            }

            // 内存使用建议
            var memoryUsageMB = systemInfo.MemoryUsedBytes / (1024.0 * 1024.0);
            if (memoryUsageMB > 2000)
            {
                recommendations.Add($"内存使用量较高({memoryUsageMB:F1}MB)，建议检查内存泄露");
            }
            else if (memoryUsageMB > 1000)
            {
                recommendations.Add($"内存使用量偏高({memoryUsageMB:F1}MB)，建议监控内存使用趋势");
            }

            // 线程数建议
            if (systemInfo.ThreadCount > 200)
            {
                recommendations.Add($"系统线程数较多({systemInfo.ThreadCount})，建议检查线程池配置");
            }

            // GC建议
            var totalGC = systemInfo.GcGen0Collections + systemInfo.GcGen1Collections + systemInfo.GcGen2Collections;
            if (totalGC > 1000)
            {
                recommendations.Add($"GC回收次数较多({totalGC})，建议优化对象生命周期管理");
            }

            if (recommendations.Count == 0)
            {
                recommendations.Add("系统运行状态良好，无特殊优化建议");
            }

            return recommendations;
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