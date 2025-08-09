using System.Text.Json;

namespace LYBT.Infrastructure.Performance.Cache
{
    /// <summary>
    /// 统一缓存管理接口 - UltraThink性能优化
    /// </summary>
    public interface IUnifiedCacheManager
    {
        /// <summary>
        /// 获取缓存项
        /// </summary>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// 设置缓存项
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// 获取或设置缓存项
        /// </summary>
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// 移除缓存项
        /// </summary>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除模式匹配的缓存项
        /// </summary>
        Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量设置缓存项
        /// </summary>
        Task SetBatchAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// 批量获取缓存项
        /// </summary>
        Task<Dictionary<string, T?>> GetBatchAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// 检查缓存项是否存在
        /// </summary>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        Task ClearAllAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// 缓存命中次数
        /// </summary>
        public long HitCount { get; set; }

        /// <summary>
        /// 缓存未命中次数
        /// </summary>
        public long MissCount { get; set; }

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double HitRate => HitCount + MissCount == 0 ? 0 : (double)HitCount / (HitCount + MissCount);

        /// <summary>
        /// 缓存项总数
        /// </summary>
        public long TotalKeys { get; set; }

        /// <summary>
        /// 内存使用量（字节）
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// 过期清理次数
        /// </summary>
        public long EvictionCount { get; set; }
    }

    /// <summary>
    /// 缓存操作选项
    /// </summary>
    public class CacheOptions
    {
        /// <summary>
        /// 默认过期时间
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// 是否压缩数据
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// 压缩阈值（字节）
        /// </summary>
        public int CompressionThreshold { get; set; } = 1024;

        /// <summary>
        /// 序列化选项
        /// </summary>
        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
}