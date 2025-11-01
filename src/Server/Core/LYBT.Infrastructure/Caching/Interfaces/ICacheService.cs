#nullable enable

using LYBT.Infrastructure.Caching.Models;

namespace LYBT.Infrastructure.Caching.Interfaces
{
    /// <summary>
    /// 统一缓存服务接口 - Infrastructure层核心缓存抽象（MVP简化版）
    /// </summary>
    /// <remarks>
    /// <para>MVP原则: 仅保留实际使用的方法，避免YAGNI（You Aren't Gonna Need It）</para>
    /// <para>架构位置: Infrastructure层，作为缓存服务的核心抽象</para>
    /// <para>实现方式: MemoryCacheAdapter（IMemoryCache适配器）</para>
    /// <para>简化历史: Issue #1745 - 从256行35方法简化为76行6方法</para>
    /// </remarks>
    public interface ICacheService
    {
        #region 核心已使用方法（3个）- CacheHealthController实际调用

        /// <summary>
        /// 清空所有缓存（同步）
        /// </summary>
        /// <remarks>
        /// <para>用途: 系统重启、配置重载、内存清理</para>
        /// <para>调用方: CacheHealthController.ClearCache() - DELETE /api/v1/system/cache/clear</para>
        /// </remarks>
        void Clear();

        /// <summary>
        /// 获取缓存统计信息（异步）
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>缓存统计数据（总键数、命中率、内存使用等）</returns>
        /// <remarks>
        /// <para>调用方: CacheHealthController.GetStatistics() - GET /api/v1/system/cache/statistics</para>
        /// </remarks>
        Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 按模式移除缓存（异步）
        /// </summary>
        /// <param name="pattern">模式字符串，支持通配符 * 和 ?</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>移除的缓存项数量</returns>
        /// <remarks>
        /// <para>示例: RemoveByPatternAsync("users:*") 清理所有用户缓存</para>
        /// <para>调用方: CacheHealthController.ClearCacheByPattern() - DELETE /api/v1/system/cache/clear-pattern</para>
        /// </remarks>
        Task<int> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

        #endregion

        #region 基础CRUD方法（3个）- 扩展预留，供未来业务模块使用

        /// <summary>
        /// 获取缓存项（异步）
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存键，支持命名空间前缀如 "users:123"</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>缓存的数据项，不存在时返回default(T)</returns>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置缓存项（异步）
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存数据</param>
        /// <param name="expiration">过期时间，null使用默认10分钟</param>
        /// <param name="priority">缓存优先级，决定内存压力时的清理顺序</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步操作任务</returns>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CachePriority priority = CachePriority.Normal, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除缓存项（异步）
        /// </summary>
        /// <param name="key">要移除的缓存键</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>true: 成功移除; false: 键不存在</returns>
        Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

        #endregion
    }
}
