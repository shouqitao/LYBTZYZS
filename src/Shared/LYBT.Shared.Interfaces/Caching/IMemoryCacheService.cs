using System;
using System.Threading.Tasks;

namespace LYBT.Shared.Interfaces.Caching
{
    /// <summary>
    /// 内存缓存服务接口 - 统一定义
    /// ⚠️ [UltraThink Phase 4] 此接口已被简化，建议迁移到 ISimplifiedCacheService
    /// 新接口将14个方法简化为8个核心方法，提升开发效率
    /// </summary>
    [Obsolete("此接口过于复杂，请迁移到 ISimplifiedCacheService。新接口提供8个核心方法，涵盖所有常用场景。", false)]
    public interface IMemoryCacheService
    {
        T Get<T>(string key);
        Task<T> GetAsync<T>(string key);
        void Set<T>(string key, T value, TimeSpan? expiration = null);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        void Set<T>(string key, T value, CacheOptions options);
        Task SetAsync<T>(string key, T value, CacheOptions options);
        bool TryGetValue<T>(string key, out T value);
        Task<(bool exists, T value)> TryGetValueAsync<T>(string key);
        void Remove(string key);
        Task RemoveAsync(string key);
        void Clear();
        Task ClearAsync();
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheOptions options);
    }

    /// <summary>
    /// 缓存选项
    /// </summary>
    public class CacheOptions
    {
        public TimeSpan? AbsoluteExpiration { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public CachePriority Priority { get; set; } = CachePriority.Normal;
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);
        
        public static readonly CacheOptions ShortTerm = new() { Duration = TimeSpan.FromMinutes(5) };
        public static readonly CacheOptions MediumTerm = new() { Duration = TimeSpan.FromMinutes(30) };
        public static readonly CacheOptions LongTerm = new() { Duration = TimeSpan.FromHours(2) };
    }

    /// <summary>
    /// 缓存优先级
    /// </summary>
    public enum CachePriority
    {
        Low,
        Normal,
        High
    }
}