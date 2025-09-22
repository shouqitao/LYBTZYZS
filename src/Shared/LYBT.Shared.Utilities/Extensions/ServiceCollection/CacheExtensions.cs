using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.Shared.Utilities.Extensions.ServiceCollection
{
    /// <summary>
    /// 缓存管理扩展方法
    /// </summary>
    public static class CacheExtensions
    {
        /// <summary>
        /// 获取或设置缓存项（异步）
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="cache">内存缓存实例</param>
        /// <param name="key">缓存键</param>
        /// <param name="factory">缓存项生成工厂方法</param>
        /// <param name="expiration">过期时间（可选）</param>
        /// <returns>缓存项值</returns>
        public static async Task<T?> GetOrSetAsync<T>(
            this IMemoryCache cache,
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiration = null)
        {
            if (cache.TryGetValue<T>(key, out var cachedValue))
            {
                return cachedValue;
            }

            var value = await factory();

            if (value != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions();

                if (expiration.HasValue)
                {
                    cacheEntryOptions.SetSlidingExpiration(expiration.Value);
                }
                else
                {
                    // 默认滑动过期时间为30分钟
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(30));
                }

                cache.Set(key, value, cacheEntryOptions);
            }

            return value;
        }

        /// <summary>
        /// 获取或设置缓存项（同步）
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="cache">内存缓存实例</param>
        /// <param name="key">缓存键</param>
        /// <param name="factory">缓存项生成工厂方法</param>
        /// <param name="expiration">过期时间（可选）</param>
        /// <returns>缓存项值</returns>
        public static T? GetOrSet<T>(
            this IMemoryCache cache,
            string key,
            Func<T> factory,
            TimeSpan? expiration = null)
        {
            if (cache.TryGetValue<T>(key, out var cachedValue))
            {
                return cachedValue;
            }

            var value = factory();

            if (value != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions();

                if (expiration.HasValue)
                {
                    cacheEntryOptions.SetSlidingExpiration(expiration.Value);
                }
                else
                {
                    // 默认滑动过期时间为30分钟
                    cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromMinutes(30));
                }

                cache.Set(key, value, cacheEntryOptions);
            }

            return value;
        }

        /// <summary>
        /// 根据模式移除缓存项
        /// </summary>
        /// <param name="cache">内存缓存实例</param>
        /// <param name="pattern">匹配模式（支持*通配符）</param>
        public static void RemoveByPattern(this IMemoryCache cache, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return;

            var keysToRemove = GetAllCacheKeys(cache)
                .Where(key => IsPatternMatch(key, pattern))
                .ToList();

            foreach (var key in keysToRemove)
            {
                cache.Remove(key);
            }
        }

        /// <summary>
        /// 根据前缀移除缓存项
        /// </summary>
        /// <param name="cache">内存缓存实例</param>
        /// <param name="prefix">键前缀</param>
        public static void RemoveByPrefix(this IMemoryCache cache, string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return;

            var keysToRemove = GetAllCacheKeys(cache)
                .Where(key => key.StartsWith(prefix))
                .ToList();

            foreach (var key in keysToRemove)
            {
                cache.Remove(key);
            }
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        /// <param name="cache">内存缓存实例</param>
        public static void Clear(this IMemoryCache cache)
        {
            if (cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0);
            }
        }

        /// <summary>
        /// 获取所有缓存键
        /// </summary>
        /// <param name="cache">内存缓存实例</param>
        /// <returns>缓存键列表</returns>
        private static IEnumerable<string> GetAllCacheKeys(IMemoryCache cache)
        {
            var keys = new List<string>();

            if (cache is MemoryCache memoryCache)
            {
                // 使用反射访问内部集合（仅用于调试和管理目的）
                var field = typeof(MemoryCache).GetProperty("EntriesCollection",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (field?.GetValue(memoryCache) is ICollection entries)
                {
                    foreach (var entry in entries)
                    {
                        var keyProperty = entry.GetType().GetProperty("Key");
                        if (keyProperty?.GetValue(entry) is string key)
                        {
                            keys.Add(key);
                        }
                    }
                }
            }

            return keys;
        }

        /// <summary>
        /// 检查键是否匹配模式
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="pattern">匹配模式</param>
        /// <returns>是否匹配</returns>
        private static bool IsPatternMatch(string key, string pattern)
        {
            // 简单的通配符匹配实现
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            return System.Text.RegularExpressions.Regex.IsMatch(key, regexPattern);
        }
    }
}
