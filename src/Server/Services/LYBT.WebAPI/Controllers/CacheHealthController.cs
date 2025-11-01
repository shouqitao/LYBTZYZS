using LYBT.Infrastructure.Caching.Interfaces;
using LYBT.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 缓存管理控制器 - MVP简化版（Issue #1733 Task 1.3）
    /// </summary>
    [ApiController]
    [Route("api/v1/system/cache")]
    [Authorize(Roles = "Admin")]
    public class CacheHealthController : BaseSystemController
    {
        private readonly ICacheService _cacheService;

        public CacheHealthController(
            ICacheService cacheService,
            ILogger<CacheHealthController> logger)
            : base(logger)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
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
