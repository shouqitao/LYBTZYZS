// ---------------------------------------------------------------------------
// DesktopCacheKeyRegistry — 桌面缓存键登记表（去反射的前缀失效）
// ---------------------------------------------------------------------------
// 背景：既有 IMemoryCache.RemoveByPrefix 依赖反射 MemoryCache 私有属性 EnumerateEntries，
// 实现变更时会静默返回空集合（失效无声失败，见架构审查 L3 缓存策略）。
// 本登记表在写入缓存时登记键，并通过 MemoryCacheEntryOptions 的逐出回调保持同步：
//   - 条目被逐出/过期/替换 → 回调移除登记
//   - 前缀失效 → 只遍历登记表，不触碰缓存内部结构
//
// 单例注册（Shell DI），与 IMemoryCache 同生命周期。
// ---------------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.Desktop.Foundation.Caching;

/// <summary>
/// 桌面缓存键登记表 —— 为 <see cref="IMemoryCache"/> 提供无需反射的前缀失效能力。
/// </summary>
public sealed class DesktopCacheKeyRegistry
{
    private readonly ConcurrentDictionary<string, byte> _keys = new(StringComparer.Ordinal);

    /// <summary>
    /// 登记一个缓存键，并把「键被逐出时自动注销」挂到 <paramref name="options"/> 上。
    /// </summary>
    /// <param name="key">缓存键（非空）。</param>
    /// <param name="options">即将用于写入的缓存条目选项（原地追加逐出回调）。</param>
    /// <exception cref="ArgumentNullException">任一参数为 null。</exception>
    public void Track(string key, MemoryCacheEntryOptions options)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(options);

        options.RegisterPostEvictionCallback(static (evictedKey, _, _, state) =>
        {
            if (evictedKey is string k && state is DesktopCacheKeyRegistry registry)
                registry._keys.TryRemove(k, out _);
        }, this);

        _keys.TryAdd(key, 0);
    }

    /// <summary>
    /// 使所有以 <paramref name="prefix"/> 开头的缓存条目失效。
    /// </summary>
    /// <param name="cache">缓存实例。</param>
    /// <param name="prefix">键前缀（例：<c>GET:/api/v1/patients</c>）。</param>
    /// <returns>被移除的条目数。</returns>
    public int RemoveByPrefix(IMemoryCache cache, string prefix)
    {
        ArgumentNullException.ThrowIfNull(cache);
        if (string.IsNullOrEmpty(prefix))
            return 0;

        var removed = 0;
        foreach (var key in _keys.Keys)
        {
            if (!key.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            cache.Remove(key);
            _keys.TryRemove(key, out _);
            removed++;
        }

        return removed;
    }

    /// <summary>
    /// 清空全部已登记条目。
    /// </summary>
    /// <param name="cache">缓存实例。</param>
    /// <returns>被移除的条目数。</returns>
    public int Clear(IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);

        var removed = 0;
        foreach (var key in _keys.Keys)
        {
            cache.Remove(key);
            _keys.TryRemove(key, out _);
            removed++;
        }

        return removed;
    }

    /// <summary>当前登记键数量（诊断用）。</summary>
    public int Count => _keys.Count;
}
