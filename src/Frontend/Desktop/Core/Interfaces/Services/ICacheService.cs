using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Cache;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 缓存服务接口 - 提供企业级标准化缓存功能
    /// </summary>
    public interface ICacheService
    {
        #region 同步方法

        /// <summary>
        /// 获取缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <returns>缓存项，如果不存在返回默认值</returns>
        T? Get<T>(string key);

        /// <summary>
        /// 设置缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expiration">过期时间</param>
        void Set<T>(string key, T value, TimeSpan expiration);

        /// <summary>
        /// 设置缓存项（使用缓存策略）
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="policy">缓存策略</param>
        void Set<T>(string key, T value, CachePolicy policy);

        /// <summary>
        /// 尝试获取缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="value">输出缓存值</param>
        /// <returns>是否获取成功</returns>
        bool TryGet<T>(string key, out T? value);

        /// <summary>
        /// 移除缓存项
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否移除成功</returns>
        bool Remove(string key);

        /// <summary>
        /// 检查缓存项是否存在
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns>是否存在</returns>
        bool Exists(string key);

        #endregion

        #region 异步方法

        /// <summary>
        /// 异步获取或创建缓存项
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">创建工厂方法</param>
        /// <param name="expiration">过期时间</param>
        /// <returns>缓存项</returns>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);

        /// <summary>
        /// 异步获取或创建缓存项（使用缓存策略）
        /// </summary>
        /// <typeparam name="T">缓存项类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">创建工厂方法</param>
        /// <param name="policy">缓存策略</param>
        /// <returns>缓存项</returns>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, CachePolicy policy);

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量设置缓存项
        /// </summary>
        /// <param name="items">缓存项字典</param>
        /// <param name="expiration">过期时间</param>
        void SetMany(Dictionary<string, object> items, TimeSpan expiration);

        /// <summary>
        /// 批量获取缓存项
        /// </summary>
        /// <param name="keys">缓存键列表</param>
        /// <returns>缓存项字典</returns>
        Dictionary<string, object?> GetMany(IEnumerable<string> keys);

        /// <summary>
        /// 批量移除缓存项
        /// </summary>
        /// <param name="keys">缓存键列表</param>
        /// <returns>移除成功的数量</returns>
        int RemoveMany(IEnumerable<string> keys);

        #endregion

        #region 缓存管理

        /// <summary>
        /// 按模式移除缓存项
        /// </summary>
        /// <param name="pattern">匹配模式（支持通配符*）</param>
        /// <returns>移除成功的数量</returns>
        int RemoveByPattern(string pattern);

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        void Clear();

        /// <summary>
        /// 清空指定分区的缓存
        /// </summary>
        /// <param name="partition">分区名称</param>
        void ClearPartition(string partition);

        /// <summary>
        /// 触发缓存清理（移除过期项）
        /// </summary>
        /// <returns>清理的项数</returns>
        int Cleanup();

        #endregion

        #region 统计与监控

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        CacheStatistics GetStatistics();

        /// <summary>
        /// 重置统计信息
        /// </summary>
        void ResetStatistics();

        /// <summary>
        /// 获取所有缓存键
        /// </summary>
        /// <returns>缓存键列表</returns>
        IEnumerable<string> GetAllKeys();

        /// <summary>
        /// 获取缓存项数量
        /// </summary>
        /// <returns>项数</returns>
        int Count { get; }

        #endregion
    }

}