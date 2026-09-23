using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Caching;

/// <summary>
/// 缓存失效服务 -- MemoryCache 前缀清理
/// P-01: 原 IOutputCacheStore 驱逐已移除——全仓 0 处 [OutputCache] 特性，OutputCache 属空转基建；
/// 实际生效的失效走 <see cref="ServerCacheKeyRegistry.RemoveByPrefix"/>（约定: MemoryCache key 以 tag 为前缀，
/// 写入时经 <see cref="ServerCacheKeyRegistry.Track"/> 登记）——不再反射 MemoryCache 私有集合。
/// </summary>
public sealed class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ServerCacheKeyRegistry _registry;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        IMemoryCache memoryCache,
        ServerCacheKeyRegistry registry,
        ILogger<CacheInvalidationService> logger)
    {
        _memoryCache = memoryCache;
        _registry = registry;
        _logger = logger;
    }

    public Task InvalidateAsync(string tag, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[Cache] Invalidating tag={Tag}", tag);

        // MemoryCache: 按前缀清理 (约定: MemoryCache key 以 tag 为前缀，写入时经 registry.Track 登记)
        var removed = _registry.RemoveByPrefix(_memoryCache, tag);
        _logger.LogDebug("[Cache] Tag={Tag} invalidated - removed {Count} entries", tag, removed);

        return Task.CompletedTask;
    }

    public async Task InvalidateAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        foreach (var tag in tags)
        {
            await InvalidateAsync(tag, cancellationToken);
        }
    }
}
