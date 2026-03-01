using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Utilities.Extensions.ServiceCollection;

namespace LYBT.Infrastructure.Caching;

/// <summary>
/// 缓存失效服务 -- 聚合 OutputCache Tag 失效 + MemoryCache 前缀清理
/// </summary>
public sealed class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IOutputCacheStore _outputCacheStore;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        IOutputCacheStore outputCacheStore,
        IMemoryCache memoryCache,
        ILogger<CacheInvalidationService> logger)
    {
        _outputCacheStore = outputCacheStore;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task InvalidateAsync(string tag, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[Cache] Invalidating tag={Tag}", tag);

        // 1. OutputCache: 按 tag 驱逐
        await _outputCacheStore.EvictByTagAsync(tag, cancellationToken);

        // 2. MemoryCache: 按前缀清理 (约定: MemoryCache key 以 tag 为前缀)
        _memoryCache.RemoveByPrefix(tag);
    }

    public async Task InvalidateAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        foreach (var tag in tags)
        {
            await InvalidateAsync(tag, cancellationToken);
        }
    }
}
