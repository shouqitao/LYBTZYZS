using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Caching;

/// <summary>
/// 缓存服务接口 - 简化版本
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 获取缓存值
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// 设置缓存值
    /// </summary>
    void Set<T>(string key, T value, TimeSpan? expiry = null);

    /// <summary>
    /// 移除缓存项
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// 检查缓存项是否存在
    /// </summary>
    bool Exists(string key);

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    void Clear();

    /// <summary>
    /// 获取或创建缓存项
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);
}

/// <summary>
/// 缓存服务实现 - 基于内存缓存的简化版本
/// </summary>
public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public T? Get<T>(string key)
    {
        try
        {
            return _cache.Get<T>(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存失败: {Key}", key);
            return default;
        }
    }

    public void Set<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var options = new MemoryCacheEntryOptions
            {
                Size = 1  // 每个条目占1个单位,配合ServiceRegistration中的SizeLimit配置
            };

            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry.Value;
            }
            else
            {
                // 默认1小时过期
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            }

            _cache.Set(key, value, options);
            _logger.LogDebug("设置缓存: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存失败: {Key}", key);
        }
    }

    public void Remove(string key)
    {
        try
        {
            _cache.Remove(key);
            _logger.LogDebug("移除缓存: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除缓存失败: {Key}", key);
        }
    }

    public bool Exists(string key)
    {
        try
        {
            return _cache.TryGetValue(key, out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查缓存存在性失败: {Key}", key);
            return false;
        }
    }

    public void Clear()
    {
        try
        {
            // 内存缓存没有直接的Clear方法，需要使用反射或重新创建
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0); // 压缩所有项
            }
            _logger.LogInformation("清空缓存完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空缓存失败");
        }
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
    {
        try
        {
            if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
            {
                _logger.LogDebug("缓存命中: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("缓存未命中，创建新值: {Key}", key);
            var value = await factory();
            Set(key, value, expiry);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取或创建缓存失败: {Key}", key);
            throw;
        }
    }
}
