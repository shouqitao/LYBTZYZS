using LYBT.Infrastructure.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 缓存管理控制器 - MVP简化版（Issue #1754：直接使用IMemoryCache）
    /// </summary>
    [ApiController]
    [Route("api/v1/system/cache")]
    [Authorize(Roles = "Admin")]
    public class CacheHealthController : BaseSystemController
    {
        private readonly IMemoryCache _memoryCache;
        private static readonly ConcurrentDictionary<string, DateTime> _cacheKeys = new();

        public CacheHealthController(
            IMemoryCache memoryCache,
            ILogger<CacheHealthController> logger)
            : base(logger)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        /// <summary>
        /// 设置缓存并跟踪键（内部辅助方法）
        /// </summary>
        /// <remarks>
        /// 注意：此方法仅供CacheHealthController内部使用
        /// 其他地方使用IMemoryCache不会被此Controller跟踪
        /// </remarks>
        private void SetCacheWithTracking<T>(string key, T value, TimeSpan? expiration = null)
        {
            var options = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration;
            }

            // 注册过期回调，自动从跟踪字典中移除
            options.RegisterPostEvictionCallback((k, v, reason, state) =>
            {
                if (k?.ToString() is string keyStr)
                {
                    _cacheKeys.TryRemove(keyStr, out _);
                }
            });

            _memoryCache.Set(key, value, options);
            _cacheKeys.TryAdd(key, DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(10)));
        }

        /// <summary>
        /// 获取缓存统计信息（MVP简化版）
        /// </summary>
        /// <returns>当前缓存统计</returns>
        /// <remarks>
        /// Issue #1754: 简化实现，只返回基本键数统计
        /// IMemoryCache不提供完整统计API，详细统计需要Redis等专业缓存
        /// </remarks>
        [HttpGet("statistics")]
        public IActionResult GetStatistics()
        {
            try
            {
                LogOperation("获取缓存统计", null, null);

                var totalKeys = _cacheKeys.Count;

                var response = new
                {
                    summary = new
                    {
                        totalKeys,
                        message = "MVP简化版：仅提供键数统计"
                    },
                    timestamp = DateTime.UtcNow
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
        public IActionResult ClearCache()
        {
            try
            {
                if (!IsSystemAdmin())
                {
                    return SystemError("需要系统管理员权限", 403);
                }

                LogOperation("清空缓存", null, null);

                var beforeKeys = _cacheKeys.Count;

                // 清空所有缓存项
                var keysToRemove = _cacheKeys.Keys.ToList();
                foreach (var key in keysToRemove)
                {
                    _memoryCache.Remove(key);
                }
                _cacheKeys.Clear();

                var response = new
                {
                    clearedItems = beforeKeys,
                    beforeKeys,
                    afterKeys = 0,
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
        /// <param name="pattern">缓存键模式，支持通配符（*和?）</param>
        /// <returns>操作结果</returns>
        [HttpDelete("clear-pattern")]
        public IActionResult ClearByPattern([FromQuery] string pattern)
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

                // 将通配符转换为正则表达式
                var regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";
                var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

                // 查找匹配的键
                var keysToRemove = _cacheKeys.Keys.Where(k => regex.IsMatch(k)).ToList();

                // 移除匹配的缓存项
                foreach (var key in keysToRemove)
                {
                    _memoryCache.Remove(key);
                    _cacheKeys.TryRemove(key, out _);
                }

                var response = new
                {
                    pattern,
                    removedCount = keysToRemove.Count,
                    operationTime = DateTime.UtcNow
                };

                return SystemOk(response, $"已清除{keysToRemove.Count}个匹配的缓存项");
            }
            catch (Exception ex)
            {
                return HandleSystemException(ex, "按模式清除缓存", new { pattern });
            }
        }
    }
}
