#nullable enable

using System.ComponentModel;
using LYBT.Core.Infrastructure.Caching.Models;

namespace LYBT.Core.Infrastructure.Caching.Interfaces
{
    /// <summary>
    /// 统一缓存服务接口 - Infrastructure层核心缓存抽象
    /// </summary>
    /// <remarks>
    /// <para>设计目标: Phase 1缓存接口收口，统一前后端缓存抽象</para>
    /// <para>架构位置: Infrastructure层，作为缓存服务的核心抽象</para>
    /// <para>适配策略: 支持Memory/Redis/Hybrid多种缓存实现</para>
    /// <para>统一完成: Pass 9 - Cache Phase 3 彻底统一缓存接口</para>
    /// </remarks>
    [Description("统一缓存服务 - Infrastructure层核心抽象")]
    public interface ICacheService
    {
        #region 同步操作 - 高频访问优化

        /// <summary>
        /// 获取缓存项 (同步)
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存键 - 支持命名空间前缀，如 "users:123", "herbs:active"</param>
        /// <returns>缓存的数据项，不存在时返回default(T)</returns>
        /// <remarks>
        /// <para>性能: 内存缓存微秒级响应，Redis毫秒级响应</para>
        /// <para>适用: 高频访问的简单数据，如用户基本信息、配置数据</para>
        /// <para>线程安全: 支持并发读取</para>
        /// </remarks>
        T? Get<T>(string key);

        /// <summary>
        /// 设置缓存项 (同步)
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存数据</param>
        /// <param name="expiration">过期时间 - null表示使用默认过期策略</param>
        /// <param name="priority">缓存优先级 - 决定内存压力时的清理顺序</param>
        /// <remarks>
        /// <para>策略: 支持滑动过期和绝对过期</para>
        /// <para>存储: 自动序列化复杂对象</para>
        /// <para>淘汰: 基于优先级的LRU策略，内存压力自动清理</para>
        /// </remarks>
        void Set<T>(string key, T value, TimeSpan? expiration = null, CachePriority priority = CachePriority.Normal);

        /// <summary>
        /// 移除缓存项 (同步)
        /// </summary>
        /// <param name="key">要移除的缓存键</param>
        /// <returns>true: 成功移除; false: 键不存在</returns>
        bool Remove(string key);

        /// <summary>
        /// 清空所有缓存 (同步)
        /// </summary>
        /// <remarks>
        /// <para>影响: 清空当前实例的所有缓存数据</para>
        /// <para>场景: 系统重启、配置重载、内存清理</para>
        /// </remarks>
        void Clear();

        /// <summary>
        /// 检查缓存键是否存在 (同步)
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>true: 存在; false: 不存在或已过期</returns>
        bool Exists(string key);

        #endregion

        #region 异步操作 - 复杂数据处理

        /// <summary>
        /// 获取缓存项 (异步) - 引用类型约束版本
        /// </summary>
        /// <typeparam name="T">缓存数据类型（必须是引用类型）</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存的数据项</returns>
        Task<T> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// 获取缓存项 (异步)
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>缓存的数据项</returns>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置缓存项 (异步) - 引用类型约束版本
        /// </summary>
        /// <typeparam name="T">缓存数据类型（必须是引用类型）</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存数据</param>
        /// <param name="expiration">过期时间</param>
        /// <returns>异步操作任务</returns>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

        /// <summary>
        /// 设置缓存项 (异步)
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存数据</param>
        /// <param name="expiration">过期时间</param>
        /// <param name="priority">缓存优先级</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步操作任务</returns>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CachePriority priority = CachePriority.Normal, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除缓存项 (异步) - 无返回值版本
        /// </summary>
        /// <param name="key">要移除的缓存键</param>
        /// <returns>异步操作任务</returns>
        Task RemoveAsync(string key);

        /// <summary>
        /// 移除缓存项 (异步)
        /// </summary>
        /// <param name="key">要移除的缓存键</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>移除操作结果</returns>
        Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取或创建缓存项 (异步) - 引用类型约束版本
        /// </summary>
        /// <typeparam name="T">缓存数据类型（必须是引用类型）</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">数据工厂方法 - 缓存未命中时调用</param>
        /// <param name="expiration">过期时间</param>
        /// <returns>缓存或新获取的数据</returns>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;

        /// <summary>
        /// 获取或设置缓存项 (异步) - 核心缓存模式
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">数据工厂方法 - 缓存未命中时调用</param>
        /// <param name="expiration">过期时间</param>
        /// <param name="priority">缓存优先级</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>缓存或新获取的数据</returns>
        /// <remarks>
        /// <para>核心模式: 缓存命中返回，未命中调用工厂方法并缓存</para>
        /// <para>性能优势: 一次调用处理缓存逻辑，避免重复检查</para>
        /// <para>线程安全: 并发调用时只有一个工厂方法执行</para>
        /// </remarks>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CachePriority priority = CachePriority.Normal, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查缓存键是否存在 (异步)
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>true: 存在; false: 不存在或已过期</returns>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// 刷新缓存项的过期时间 (异步)
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="expiration">新的过期时间</param>
        /// <returns>异步操作任务</returns>
        Task RefreshAsync(string key, TimeSpan expiration);

        /// <summary>
        /// 清空所有缓存 (异步)
        /// </summary>
        /// <returns>异步操作任务</returns>
        Task ClearAsync();

        #endregion

        #region 批量操作 - 性能优化

        /// <summary>
        /// 批量获取缓存项 (异步)
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="keys">缓存键集合</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>键值对字典，不存在的键不包含在结果中</returns>
        Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量设置缓存项 (异步)
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="items">要设置的键值对</param>
        /// <param name="expiration">过期时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步操作任务</returns>
        Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量移除缓存项 (异步)
        /// </summary>
        /// <param name="keys">要移除的缓存键集合</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>成功移除的键数量</returns>
        Task<int> RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

        #endregion

        #region 模式操作 - 高级功能

        /// <summary>
        /// 按模式移除缓存 (异步)
        /// </summary>
        /// <param name="pattern">模式字符串，支持通配符 * 和 ?</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>移除的键数量</returns>
        /// <remarks>
        /// <para>适用: 按前缀/模式批量清理缓存</para>
        /// <para>示例: RemoveByPatternAsync("users:*") 清理所有用户缓存</para>
        /// <para>性能: Redis原生支持，Memory需要遍历</para>
        /// </remarks>
        Task<int> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按前缀移除缓存 (异步) - 无返回值版本
        /// </summary>
        /// <param name="prefix">前缀字符串</param>
        /// <returns>异步操作任务</returns>
        Task RemoveByPrefixAsync(string prefix);

        /// <summary>
        /// 按前缀移除缓存 (异步)
        /// </summary>
        /// <param name="prefix">前缀字符串</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>移除的键数量</returns>
        Task<int> RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

        #endregion

        #region 统计与监控

        /// <summary>
        /// 获取缓存统计信息 (异步)
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>缓存统计数据</returns>
        Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        #endregion
    }
}
