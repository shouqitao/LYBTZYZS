using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LYBT.WPF.Client.Core.Services.Performance
{
    /// <summary>
    /// 数据预加载服务接口
    /// 实现智能缓存、预测加载和内存管理
    /// </summary>
    public interface IDataPreloadService : IDisposable
    {
        /// <summary>
        /// 预加载数据范围
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="startIndex">起始索引</param>
        /// <param name="count">数据数量</param>
        /// <param name="dataProvider">数据提供器</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>预加载任务</returns>
        Task PreloadDataAsync<T>(
            string key,
            int startIndex, 
            int count,
            Func<int, int, CancellationToken, Task<IList<T>>> dataProvider,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取缓存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="index">索引</param>
        /// <returns>缓存的数据项，如果不存在则返回null</returns>
        T? GetCachedItem<T>(string key, int index) where T : class;

        /// <summary>
        /// 获取缓存数据范围
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="startIndex">起始索引</param>
        /// <param name="count">数据数量</param>
        /// <returns>缓存的数据列表</returns>
        IList<T> GetCachedRange<T>(string key, int startIndex, int count) where T : class;

        /// <summary>
        /// 智能预测下一批数据范围
        /// </summary>
        /// <param name="currentIndex">当前索引</param>
        /// <param name="scrollDirection">滚动方向：1向下，-1向上，0静止</param>
        /// <param name="viewportSize">视口大小</param>
        /// <returns>预测的数据范围(起始索引, 数量)</returns>
        (int StartIndex, int Count) PredictNextRange(int currentIndex, int scrollDirection, int viewportSize);

        /// <summary>
        /// 清理过期缓存
        /// </summary>
        /// <param name="key">缓存键，null表示清理所有</param>
        void ClearExpiredCache(string? key = null);

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计</returns>
        CacheStatistics GetCacheStatistics();

        /// <summary>
        /// 配置缓存参数
        /// </summary>
        /// <param name="maxMemoryMB">最大内存使用(MB)</param>
        /// <param name="cacheExpirationMinutes">缓存过期时间(分钟)</param>
        /// <param name="preloadMultiplier">预加载倍数</param>
        void ConfigureCache(int maxMemoryMB = 50, int cacheExpirationMinutes = 10, double preloadMultiplier = 2.0);
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// 总缓存项数
        /// </summary>
        public int TotalCacheItems { get; set; }

        /// <summary>
        /// 缓存命中次数
        /// </summary>
        public long CacheHitCount { get; set; }

        /// <summary>
        /// 缓存未命中次数
        /// </summary>
        public long CacheMissCount { get; set; }

        /// <summary>
        /// 命中率
        /// </summary>
        public double HitRatio => CacheHitCount + CacheMissCount > 0 
            ? (double)CacheHitCount / (CacheHitCount + CacheMissCount) 
            : 0.0;

        /// <summary>
        /// 内存使用量(MB)
        /// </summary>
        public double MemoryUsageMB { get; set; }

        /// <summary>
        /// 活跃预加载任务数
        /// </summary>
        public int ActivePreloadTasks { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}