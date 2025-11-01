using LYBT.Infrastructure.Caching.Interfaces;
using LYBT.Infrastructure.Caching.Models;

namespace LYBT.Infrastructure.Caching.Adapters
{
    /// <summary>
    /// 空缓存服务实现（MVP简化版），用于禁用缓存的场景
    /// </summary>
    /// <remarks>
    /// <para>用途: 测试环境或禁用缓存时使用的空实现</para>
    /// <para>简化历史: Issue #1745 - 从215行简化为72行，删除未使用的批量操作和高级模式</para>
    /// </remarks>
    public class NullCacheService : ICacheService
    {
        #region 核心已使用方法实现（3个）

        /// <summary>
        /// 清空所有缓存（空操作）
        /// </summary>
        public void Clear()
        {
            // 空操作
        }

        /// <summary>
        /// 获取缓存统计信息（返回空统计）
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

        /// <summary>
        /// 按模式移除缓存（返回0）
        /// </summary>
        public Task<int> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        #endregion

        #region 基础CRUD方法实现（3个）

        /// <summary>
        /// 获取缓存项（始终返回default）
        /// </summary>
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(default(T));
        }

        /// <summary>
        /// 设置缓存项（空操作）
        /// </summary>
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CachePriority priority = CachePriority.Normal, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 移除缓存项（始终返回false）
        /// </summary>
        public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        #endregion
    }
}
