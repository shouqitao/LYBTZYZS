using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Extensions
{

    /// <summary>
    /// 缓存服务扩展，提供统一的缓存管理
    /// </summary>
    public static class CacheExtensions
    {

        /// <summary>
        /// 获取或设置缓存
        /// </summary>
        public static async Task<T?> GetOrSetAsync<T>(
            this IMemoryCache cache,
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiration = null)
        {
            if (cache.TryGetValue(key, out T? cachedValue))
            {
                return cachedValue;
            }

            var result = await factory();
            if (result != null)
            {
                var options = new MemoryCacheEntryOptions();
                if (expiration.HasValue)
                {
                    options.SetSlidingExpiration(expiration.Value);
                }
                else
                {
                    options.SetSlidingExpiration(TimeSpan.FromMinutes(10)); // 默认10分钟
                }

                cache.Set(key, result, options);
            }

            return result;
        }

        /// <summary>
        /// 清除符合模式的缓存键
        /// </summary>
        public static void RemoveByPattern(this IMemoryCache cache, string pattern)
        {
            // 注意：IMemoryCache没有直接的按模式删除功能
            // 在生产环境中，建议使用Redis或其他支持模式匹配的缓存
            // 这里提供一个基础实现，可以根据需要扩展
            if (cache is MemoryCache memoryCache)
            {
                var field = typeof(MemoryCache).GetField(
                    "_coherentState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field?.GetValue(memoryCache) is object coherentState)
                {
                    var entriesCollection = coherentState.GetType()
                        .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (entriesCollection?.GetValue(coherentState) is IDictionary<object, object> entries)
                    {
                        var keysToRemove = new List<object>();
                        foreach (var entry in entries)
                        {
                            if (entry.Key.ToString()?.Contains(pattern) == true)
                            {
                                keysToRemove.Add(entry.Key);
                            }
                        }

                        foreach (var key in keysToRemove)
                        {
                            cache.Remove(key);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 清除指定前缀的缓存
        /// </summary>
        public static void RemoveByPrefix(this IMemoryCache cache, string prefix)
        {
            cache.RemoveByPattern(prefix);
        }
    }
}
