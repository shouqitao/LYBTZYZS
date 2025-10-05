using LYBT.Infrastructure.Caching.Interfaces;
using LYBT.Infrastructure.Caching.Models;

namespace LYBT.Infrastructure.Caching.Adapters
{
    /// <summary>
    /// 空缓存服务实现，用于禁用缓存的场景
    /// </summary>
    public class NullCacheService : ICacheService
    {
        #region 同步操作

        /// <summary>
        /// 获取缓存（始终返回null）
        /// </summary>
        public T? Get<T>(string key)
        {
            return default(T);
        }

        /// <summary>
        /// 设置缓存（空操作）
        /// </summary>
        public void Set<T>(string key, T value, TimeSpan? expiration = null, CachePriority priority = CachePriority.Normal)
        {
            // 空操作
        }

        /// <summary>
        /// 移除缓存（始终返回false）
        /// </summary>
        public bool Remove(string key)
        {
            return false;
        }

        /// <summary>
        /// 清空缓存（空操作）
        /// </summary>
        public void Clear()
        {
            // 空操作
        }

        /// <summary>
        /// 缓存是否存在（始终返回false）
        /// </summary>
        public bool Exists(string key)
        {
            return false;
        }

        #endregion

        #region 异步操作

        /// <summary>
        /// 异步获取缓存（始终返回null）- ICacheService接口方法
        /// </summary>
        public Task<T?> GetAsync<T>(string key) where T : class => Task.FromResult(default(T));

        /// <summary>
        /// 异步获取缓存（始终返回null）
        /// </summary>
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(default(T));
        }

        /// <summary>
        /// 异步获取或创建缓存（始终执行factory）- ICacheService接口方法
        /// </summary>
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
        {
            return await factory();
        }

        /// <summary>
        /// 异步设置缓存（空操作）- ICacheService接口方法
        /// </summary>
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 异步设置缓存（空操作）
        /// </summary>
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CachePriority priority = CachePriority.Normal, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 异步移除缓存（空操作）- ICacheService接口方法
        /// </summary>
        public Task RemoveAsync(string key)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 异步移除缓存（始终返回false）
        /// </summary>
        public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(false);

        /// <summary>
        /// 检查缓存是否存在（始终返回false）- ICacheService接口方法
        /// </summary>
        public Task<bool> ExistsAsync(string key) => Task.FromResult(false);

        /// <summary>
        /// 刷新缓存过期时间（空操作）- ICacheService接口方法
        /// </summary>
        public Task RefreshAsync(string key, TimeSpan expiration) => Task.CompletedTask;

        /// <summary>
        /// 清空缓存（空操作）- ICacheService接口方法
        /// </summary>
        public Task ClearAsync() => Task.CompletedTask;

        /// <summary>
        /// 异步获取或设置缓存（始终执行factory）
        /// </summary>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CachePriority priority = CachePriority.Normal, CancellationToken cancellationToken = default)
        {
            return await factory();
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量获取缓存项（返回空字典）
        /// </summary>
        public Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Dictionary<string, T?>());
        }

        /// <summary>
        /// 批量设置缓存项（空操作）
        /// </summary>
        public Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 批量移除缓存项（返回0）
        /// </summary>
        public Task<int> RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        #endregion

        #region 模式操作

        /// <summary>
        /// 按模式移除缓存（返回0）
        /// </summary>
        public Task<int> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        /// <summary>
        /// 按前缀移除缓存（返回0）- ICacheService接口方法
        /// </summary>
        public Task RemoveByPrefixAsync(string prefix)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 按前缀移除缓存（返回0）
        /// </summary>
        public Task<int> RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        #endregion

        #region 统计与监控

        /// <summary>
        /// 异步获取统计信息（返回空统计）
        /// </summary>
        public Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CacheStatistics
            {
                HitCount = 0,
                MissCount = 0,
                EvictionCount = 0,
                CurrentItemCount = 0,
                TotalMemoryUsage = 0,
                TotalKeys = 0,
                UsedMemory = 0,
                ExpiredKeys = 0,
                EvictedKeys = 0,
                EvictionRate = 0,
                MaxCapacity = 0,
                Timestamp = DateTime.UtcNow
            });
        }



        #endregion
    }
}
