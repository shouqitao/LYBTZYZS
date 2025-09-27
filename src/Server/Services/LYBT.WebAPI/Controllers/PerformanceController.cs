using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LYBT.Infrastructure.Data.Monitoring;
using LYBT.Infrastructure.Web;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 性能监控控制器
    /// 提供查询性能统计和分析功能
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize(Roles = "Admin")] // 仅管理员可访问
    public class PerformanceController : BaseApiController
    {
        private readonly IQueryStatisticsCollector _statisticsCollector;
        private readonly ILogger<PerformanceController> _logger;

        public PerformanceController(
            IQueryStatisticsCollector statisticsCollector,
            ILogger<PerformanceController> logger)
        {
            _statisticsCollector = statisticsCollector ?? throw new ArgumentNullException(nameof(statisticsCollector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取查询性能统计报告
        /// </summary>
        /// <returns>性能统计报告文本</returns>
        [HttpGet("query-statistics")]
        [ProducesResponseType(typeof(string), 200)]
        public IActionResult GetQueryStatistics()
        {
            try
            {
                var report = _statisticsCollector.GetStatisticsReport();
                _logger.LogInformation("生成查询性能统计报告");
                return Ok(new { success = true, report = report });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取查询统计报告失败");
                return StatusCode(500, new { success = false, message = "获取统计报告失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 导出查询统计数据为JSON
        /// </summary>
        /// <returns>JSON格式的统计数据</returns>
        [HttpGet("query-statistics/export")]
        [ProducesResponseType(typeof(string), 200)]
        public IActionResult ExportQueryStatistics()
        {
            try
            {
                var jsonData = _statisticsCollector.ExportStatisticsAsJson();
                _logger.LogInformation("导出查询统计数据");
                
                // 返回JSON文件下载
                var bytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
                var fileName = $"query_statistics_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                
                return File(bytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出查询统计数据失败");
                return StatusCode(500, new { success = false, message = "导出统计数据失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 清除查询统计数据
        /// </summary>
        /// <returns>操作结果</returns>
        [HttpDelete("query-statistics")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult ClearQueryStatistics()
        {
            try
            {
                _statisticsCollector.ClearStatistics();
                _logger.LogInformation("已清除查询统计数据");
                return Ok(new { success = true, message = "查询统计数据已清除" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除查询统计数据失败");
                return StatusCode(500, new { success = false, message = "清除统计数据失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取查询性能健康状态
        /// </summary>
        /// <returns>健康状态信息</returns>
        [HttpGet("health")]
        [AllowAnonymous] // 健康检查端点可匿名访问
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetPerformanceHealth()
        {
            try
            {
                // 获取统计数据的简化版本用于健康检查
                var report = _statisticsCollector.GetStatisticsReport();
                var lines = report.Split('\n');
                
                // 提取关键指标
                var totalQueries = 0;
                var avgQueryTime = 0.0;
                var slowQueryCount = 0;
                
                foreach (var line in lines)
                {
                    if (line.Contains("总查询次数:"))
                    {
                        var value = line.Split(':').Last().Trim();
                        int.TryParse(value, out totalQueries);
                    }
                    else if (line.Contains("平均查询时间:"))
                    {
                        var value = line.Split(':').Last().Replace("ms", "").Trim();
                        double.TryParse(value, out avgQueryTime);
                    }
                    else if (line.Contains("慢查询记录:"))
                    {
                        slowQueryCount++;
                    }
                }

                // 判断健康状态
                var status = "Healthy";
                var issues = new List<string>();
                
                if (avgQueryTime > 200)
                {
                    status = "Degraded";
                    issues.Add($"平均查询时间过高: {avgQueryTime:F2}ms (阈值: 200ms)");
                }
                
                if (avgQueryTime > 500)
                {
                    status = "Unhealthy";
                }
                
                if (slowQueryCount > 10)
                {
                    if (status == "Healthy") status = "Degraded";
                    issues.Add($"慢查询过多: {slowQueryCount}个");
                }

                return Ok(new
                {
                    status = status,
                    timestamp = DateTime.UtcNow,
                    metrics = new
                    {
                        totalQueries = totalQueries,
                        averageQueryTimeMs = avgQueryTime,
                        slowQueryCount = slowQueryCount
                    },
                    issues = issues,
                    recommendation = GetPerformanceRecommendation(avgQueryTime, slowQueryCount)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取性能健康状态失败");
                return Ok(new
                {
                    status = "Unknown",
                    timestamp = DateTime.UtcNow,
                    error = "无法获取性能指标"
                });
            }
        }

        /// <summary>
        /// 获取性能优化建议
        /// </summary>
        private string GetPerformanceRecommendation(double avgQueryTime, int slowQueryCount)
        {
            if (avgQueryTime < 50 && slowQueryCount < 5)
            {
                return "查询性能良好，无需优化";
            }

            var recommendations = new List<string>();

            if (avgQueryTime > 200)
            {
                recommendations.Add("考虑添加数据库索引");
                recommendations.Add("检查是否存在N+1查询问题");
                recommendations.Add("优化复杂查询逻辑");
            }

            if (slowQueryCount > 10)
            {
                recommendations.Add("分析慢查询日志，识别性能瓶颈");
                recommendations.Add("考虑使用查询结果缓存");
                recommendations.Add("检查数据库表统计信息是否需要更新");
            }

            if (avgQueryTime > 100)
            {
                recommendations.Add("考虑使用投影(Select)减少数据传输");
                recommendations.Add("检查是否正确使用了Include预加载");
            }

            return string.Join("; ", recommendations);
        }

        /// <summary>
        /// 获取实时查询性能指标（用于监控面板）
        /// </summary>
        /// <returns>实时性能指标</returns>
        [HttpGet("metrics/realtime")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetRealtimeMetrics()
        {
            try
            {
                // 这里可以集成实时监控数据
                // 目前返回基于统计收集器的数据
                var report = _statisticsCollector.GetStatisticsReport();
                var lines = report.Split('\n');
                
                // 解析并返回简化的实时指标
                return Ok(new
                {
                    success = true,
                    timestamp = DateTime.UtcNow,
                    metrics = new
                    {
                        // 这里可以扩展更多实时指标
                        lastUpdate = DateTime.UtcNow,
                        activeQueries = 0, // 需要额外实现活动查询跟踪
                        queuedQueries = 0  // 需要额外实现队列查询跟踪
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取实时指标失败");
                return StatusCode(500, new { success = false, message = "获取实时指标失败", error = ex.Message });
            }
        }
    }
}