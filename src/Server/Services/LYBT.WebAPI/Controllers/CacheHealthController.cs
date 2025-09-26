using System;
using System.Threading.Tasks;
using LYBT.Core.Infrastructure.Caching.Interfaces;
using LYBT.Core.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 缓存健康监控控制器
    /// </summary>
    [ApiController]
    [Route("api/v1/system/cache")]
    [Authorize(Roles = "Admin")]
    public class CacheHealthController : BaseSystemController
    {
        private readonly ICacheDiagnosticsService _diagnosticsService;
        private readonly ICacheService _cacheService;

        public CacheHealthController(
            ICacheDiagnosticsService diagnosticsService,
            ICacheService cacheService,
            ILogger<CacheHealthController> logger,
            IMemoryCache cache)
            : base(logger, cache)
        {
            _diagnosticsService = diagnosticsService ?? throw new ArgumentNullException(nameof(diagnosticsService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        /// <summary>
        /// 获取缓存健康状态
        /// </summary>
        /// <returns>最新的缓存健康快照</returns>
        [HttpGet("health")]
        public async Task<IActionResult> GetHealthAsync()
        {
            try
            {
                LogOperation("获取缓存健康状态", null, null);

                // 获取最新快照
                var snapshot = _diagnosticsService.GetLatestSnapshot();

                if (snapshot == null)
                {
                    // 如果没有快照，立即运行一次诊断
                    var diagnosticResult = await _diagnosticsService.RunDiagnosticsAsync();
                    snapshot = _diagnosticsService.GetLatestSnapshot();
                }

                if (snapshot == null)
                {
                    return SystemWarning(new
                    {
                        message = "尚无缓存健康数据",
                        hint = "等待后台服务首次采样或手动触发诊断"
                    }, "缓存健康数据尚未生成");
                }

                var response = new
                {
                    snapshotId = snapshot.SnapshotId,
                    snapshotTime = snapshot.SnapshotTime,
                    healthLevel = snapshot.HealthLevel.ToString(),
                    statistics = new
                    {
                        hitRate = snapshot.Statistics.HitRatio,
                        capacityUsage = snapshot.Statistics.CapacityUsageRatio,
                        evictionRate = snapshot.Statistics.EvictionRate,
                        totalKeys = snapshot.Statistics.TotalKeys,
                        currentItemCount = snapshot.Statistics.CurrentItemCount,
                        hitCount = snapshot.Statistics.HitCount,
                        missCount = snapshot.Statistics.MissCount
                    },
                    thresholds = snapshot.ThresholdCheck != null ? new
                    {
                        hitRate = new
                        {
                            current = snapshot.ThresholdCheck.CurrentHitRate,
                            threshold = snapshot.ThresholdCheck.HitRateThreshold,
                            alert = snapshot.ThresholdCheck.IsLowHitRate
                        },
                        capacity = new
                        {
                            current = snapshot.ThresholdCheck.CurrentCapacityRatio,
                            threshold = snapshot.ThresholdCheck.CapacityThreshold,
                            alert = snapshot.ThresholdCheck.IsHighCapacity
                        },
                        evictionRate = new
                        {
                            current = snapshot.ThresholdCheck.CurrentEvictionRate,
                            threshold = snapshot.ThresholdCheck.EvictionRateThreshold,
                            alert = snapshot.ThresholdCheck.IsHighEvictionRate
                        },
                        hasAnyAlert = snapshot.ThresholdCheck.HasAnyAlert
                    } : null,
                    samplingWindowSeconds = snapshot.SamplingWindowSeconds
                };

                return SystemOk(response, "缓存健康状态获取成功");
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "获取缓存健康状态");
            }
        }

        /// <summary>
        /// 运行缓存诊断
        /// </summary>
        /// <returns>详细的诊断结果</returns>
        [HttpPost("diagnose")]
        public async Task<IActionResult> RunDiagnosticsAsync()
        {
            try
            {
                if (!IsSystemAdmin())
                {
                    return SystemError("需要系统管理员权限", 403);
                }

                LogOperation("运行缓存诊断", null, null);

                var result = await _diagnosticsService.RunDiagnosticsAsync();

                var response = new
                {
                    diagnosticId = result.DiagnosticId,
                    diagnosticTime = result.DiagnosticTime,
                    elapsedMilliseconds = result.ElapsedMilliseconds,
                    healthStatus = new
                    {
                        isHealthy = result.HealthStatus.IsHealthy,
                        level = result.HealthStatus.Level.ToString(),
                        message = result.HealthStatus.Message,
                        recommendations = result.HealthStatus.Recommendations
                    },
                    performance = result.Performance != null ? new
                    {
                        hitRate = result.Performance.HitRate,
                        averageResponseTime = result.Performance.AverageResponseTime,
                        evictionRate = result.Performance.EvictionRate,
                        throughput = result.Performance.Throughput
                    } : null,
                    capacity = result.Capacity != null ? new
                    {
                        usedCapacity = result.Capacity.UsedCapacity,
                        maxCapacity = result.Capacity.MaxCapacity,
                        usageRatio = result.Capacity.UsageRatio,
                        estimatedTimeToFull = result.Capacity.EstimatedTimeToFull,
                        memoryUsageMB = result.Capacity.MemoryUsageBytes / (1024.0 * 1024.0)
                    } : null
                };

                return SystemOk(response, "缓存诊断完成");
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "运行缓存诊断");
            }
        }

        /// <summary>
        /// 获取历史快照
        /// </summary>
        /// <param name="count">获取数量，默认10</param>
        /// <returns>历史快照列表</returns>
        [HttpGet("history")]
        public IActionResult GetHistorySnapshots([FromQuery] int count = 10)
        {
            try
            {
                LogOperation("获取缓存历史快照", new { count }, null);

                var validationError = ValidateSystemParameters(
                    (count > 0 && count <= 100, "获取数量必须在1-100之间")
                );

                if (validationError != null)
                    return validationError;

                var snapshots = _diagnosticsService.GetHistorySnapshots(count);

                var response = snapshots.Select(s => new
                {
                    snapshotId = s.SnapshotId,
                    snapshotTime = s.SnapshotTime,
                    healthLevel = s.HealthLevel.ToString(),
                    hitRate = s.Statistics?.HitRatio ?? 0,
                    capacityUsage = s.Statistics?.CapacityUsageRatio ?? 0,
                    evictionRate = s.Statistics?.EvictionRate ?? 0,
                    hasAlert = s.ThresholdCheck?.HasAnyAlert ?? false
                }).ToList();

                return SystemOk(response, $"获取最近{response.Count}条历史快照");
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "获取历史快照");
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>当前缓存统计</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatisticsAsync()
        {
            try
            {
                LogOperation("获取缓存统计", null, null);

                var statistics = await _cacheService.GetStatisticsAsync();

                var response = new
                {
                    summary = new
                    {
                        totalKeys = statistics.TotalKeys,
                        totalRequests = statistics.TotalRequests,
                        hitCount = statistics.HitCount,
                        missCount = statistics.MissCount,
                        hitRatio = statistics.HitRatio
                    },
                    memory = new
                    {
                        usedMemoryMB = statistics.UsedMemory / (1024.0 * 1024.0),
                        totalMemoryUsageMB = statistics.TotalMemoryUsage / (1024.0 * 1024.0)
                    },
                    eviction = new
                    {
                        expiredKeys = statistics.ExpiredKeys,
                        evictedKeys = statistics.EvictedKeys,
                        evictionCount = statistics.EvictionCount,
                        evictionRate = statistics.EvictionRate
                    },
                    capacity = new
                    {
                        currentItemCount = statistics.CurrentItemCount,
                        maxCapacity = statistics.MaxCapacity,
                        capacityUsageRatio = statistics.CapacityUsageRatio
                    },
                    timestamp = statistics.Timestamp
                };

                return SystemOk(response, "缓存统计获取成功");
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "获取缓存统计");
            }
        }

        /// <summary>
        /// 清空缓存（危险操作）
        /// </summary>
        /// <returns>操作结果</returns>
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCacheAsync()
        {
            try
            {
                if (!IsSystemAdmin())
                {
                    return SystemError("需要系统管理员权限", 403);
                }

                LogOperation("清空缓存", null, null);

                // 获取清空前的统计
                var beforeStats = await _cacheService.GetStatisticsAsync();

                // 清空缓存
                _cacheService.Clear();

                // 获取清空后的统计
                var afterStats = await _cacheService.GetStatisticsAsync();

                var response = new
                {
                    clearedItems = beforeStats.TotalKeys,
                    clearedMemoryMB = beforeStats.UsedMemory / (1024.0 * 1024.0),
                    beforeKeys = beforeStats.TotalKeys,
                    afterKeys = afterStats.TotalKeys,
                    operationTime = DateTime.UtcNow
                };

                return SystemOk(response, "缓存已清空");
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "清空缓存");
            }
        }

        /// <summary>
        /// 按模式清除缓存
        /// </summary>
        /// <param name="pattern">缓存键模式，支持通配符</param>
        /// <returns>操作结果</returns>
        [HttpDelete("clear-pattern")]
        public async Task<IActionResult> ClearByPatternAsync([FromQuery] string pattern)
        {
            try
            {
                if (!IsSystemAdmin())
                {
                    return SystemError("需要系统管理员权限", 403);
                }

                if (string.IsNullOrWhiteSpace(pattern))
                {
                    return SystemError("模式参数不能为空", 400);
                }

                LogOperation("按模式清除缓存", new { pattern }, null);

                var removedCount = await _cacheService.RemoveByPatternAsync(pattern);

                var response = new
                {
                    pattern,
                    removedCount,
                    operationTime = DateTime.UtcNow
                };

                return SystemOk(response, $"已清除{removedCount}个匹配的缓存项");
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "按模式清除缓存", new { pattern });
            }
        }
    }
}