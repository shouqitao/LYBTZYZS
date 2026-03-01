using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.Shared.Utilities.Extensions.ServiceCollection;

/// <summary>
/// IMemoryCache 扩展方法 -- 按前缀清除和全量清除
/// </summary>
public static class CacheExtensions
{
    /// <summary>
    /// 根据前缀移除缓存项
    /// </summary>
    public static void RemoveByPrefix(this IMemoryCache cache, string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return;

        var keysToRemove = GetAllCacheKeys(cache)
            .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        foreach (var key in keysToRemove)
        {
            cache.Remove(key);
        }
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    public static void Clear(this IMemoryCache cache)
    {
        if (cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }
    }

    /// <summary>
    /// 获取所有缓存键 (反射访问 MemoryCache 内部集合)
    /// </summary>
    private static IEnumerable<string> GetAllCacheKeys(IMemoryCache cache)
    {
        if (cache is not MemoryCache memoryCache)
            return [];

        var field = typeof(MemoryCache).GetProperty(
            "EntriesCollection",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (field?.GetValue(memoryCache) is not ICollection entries)
            return [];

        var keys = new List<string>();
        foreach (var entry in entries)
        {
            var keyProperty = entry.GetType().GetProperty("Key");
            if (keyProperty?.GetValue(entry) is string key)
            {
                keys.Add(key);
            }
        }

        return keys;
    }
}
