using System;
using System.Threading.Tasks;

namespace LYBT.Shared.Interfaces.Caching
{
    /// <summary>
    /// 简化缓存服务接口 - UltraThink Phase 4优化
    /// 将原有14个方法简化为8个核心方法，提升开发效率
    /// </summary>
    public interface ISimplifiedCacheService
    {
        /// <summary>
        /// 获取缓存项（同步）
        /// </summary>
        T? Get<T>(string key);

        /// <summary>
        /// 设置缓存项（同步）
        /// </summary>
        void Set<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// 移除缓存项（同步）
        /// </summary>
        bool Remove(string key);

        /// <summary>
        /// 清空所有缓存（同步）
        /// </summary>
        void Clear();

        /// <summary>
        /// 获取缓存项（异步）
        /// </summary>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// 设置缓存项（异步）
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// 移除缓存项（异步）
        /// </summary>
        Task<bool> RemoveAsync(string key);

        /// <summary>
        /// 获取或设置缓存项（异步，核心方法）
        /// </summary>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    }
}