using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// 智能数据预加载服务实现
    /// 支持滚动预测、内存管理、异步缓存
    /// </summary>
    public class DataPreloadService : IDataPreloadService, IDisposable
    {
        private readonly ConcurrentDictionary<string, CacheContainer> _caches = new();
        private readonly ConcurrentDictionary<string, Task> _preloadTasks = new();
        private readonly Timer _cleanupTimer;
        private readonly SemaphoreSlim _memoryManagementSemaphore = new(1, 1);

        // 配置参数
        private int _maxMemoryMB = 50;
        private int _cacheExpirationMinutes = 10;
        private double _preloadMultiplier = 2.0;

        // 统计信息
        private long _cacheHitCount;
        private long _cacheMissCount;

        /// <summary>
        /// 缓存容器
        /// </summary>
        private class CacheContainer
        {
            public ConcurrentDictionary<int, CacheItem> Items { get; } = new();
            public DateTime LastAccessTime { get; set; } = DateTime.Now;
            public long EstimatedMemoryBytes { get; set; }

            public class CacheItem
            {
                public object? Data { get; set; }
                public DateTime CachedTime { get; set; } = DateTime.Now;
                public DateTime LastAccessTime { get; set; } = DateTime.Now;
                public long EstimatedSizeBytes { get; set; }
            }
        }

        public DataPreloadService()
        {
            // 每分钟清理一次过期缓存
            _cleanupTimer = new Timer(CleanupCallback, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public async Task PreloadDataAsync<T>(
            string key,
            int startIndex,
            int count,
            Func<int, int, CancellationToken, Task<IList<T>>> dataProvider,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key) || count <= 0 || dataProvider == null)
                return;

            var taskKey = $"{key}_{startIndex}_{count}";
            
            // 避免重复的预加载任务
            if (_preloadTasks.ContainsKey(taskKey))
                return;

            var preloadTask = Task.Run(async () =>
            {
                try
                {
                    // 检查内存使用情况
                    await ManageMemoryUsageAsync();

                    var data = await dataProvider(startIndex, count, cancellationToken);
                    if (data?.Count > 0)
                    {
                        CacheDataRange(key, startIndex, data);
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine($"[DataPreloadService] 预加载任务被取消: {taskKey}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DataPreloadService] 预加载任务失败: {taskKey}, 错误: {ex.Message}");
                }
                finally
                {
                    _preloadTasks.TryRemove(taskKey, out _);
                }
            }, cancellationToken);

            _preloadTasks[taskKey] = preloadTask;
            await preloadTask;
        }

        public T? GetCachedItem<T>(string key, int index) where T : class
        {
            if (string.IsNullOrEmpty(key) || !_caches.TryGetValue(key, out var cache))
            {
                Interlocked.Increment(ref _cacheMissCount);
                return null;
            }

            if (cache.Items.TryGetValue(index, out var cacheItem))
            {
                cacheItem.LastAccessTime = DateTime.Now;
                cache.LastAccessTime = DateTime.Now;
                Interlocked.Increment(ref _cacheHitCount);
                return cacheItem.Data as T;
            }

            Interlocked.Increment(ref _cacheMissCount);
            return null;
        }

        public IList<T> GetCachedRange<T>(string key, int startIndex, int count) where T : class
        {
            var result = new List<T>();
            
            if (string.IsNullOrEmpty(key) || count <= 0)
                return result;

            for (int i = 0; i < count; i++)
            {
                var item = GetCachedItem<T>(key, startIndex + i);
                if (item != null)
                {
                    result.Add(item);
                }
                else
                {
                    break; // 连续缓存中断，停止获取
                }
            }

            return result;
        }

        public (int StartIndex, int Count) PredictNextRange(int currentIndex, int scrollDirection, int viewportSize)
        {
            var preloadCount = Math.Max(1, (int)(viewportSize * _preloadMultiplier));
            
            switch (scrollDirection)
            {
                case 1: // 向下滚动
                    return (currentIndex + viewportSize, preloadCount);
                
                case -1: // 向上滚动
                    var startIndex = Math.Max(0, currentIndex - preloadCount);
                    return (startIndex, preloadCount);
                
                default: // 静止或未知方向
                    // 预加载当前位置前后的数据
                    var halfPreload = preloadCount / 2;
                    startIndex = Math.Max(0, currentIndex - halfPreload);
                    return (startIndex, preloadCount);
            }
        }

        public void ClearExpiredCache(string? key = null)
        {
            var expiration = TimeSpan.FromMinutes(_cacheExpirationMinutes);
            var now = DateTime.Now;

            if (string.IsNullOrEmpty(key))
            {
                // 清理所有过期缓存
                var expiredKeys = _caches
                    .Where(kvp => now - kvp.Value.LastAccessTime > expiration)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var expiredKey in expiredKeys)
                {
                    if (_caches.TryRemove(expiredKey, out var expiredCache))
                    {
                        Debug.WriteLine($"[DataPreloadService] 清理过期缓存: {expiredKey}，项目数: {expiredCache.Items.Count}");
                    }
                }
            }
            else
            {
                // 清理指定缓存
                if (_caches.TryRemove(key, out var cache))
                {
                    Debug.WriteLine($"[DataPreloadService] 清理缓存: {key}，项目数: {cache.Items.Count}");
                }
            }
        }

        public CacheStatistics GetCacheStatistics()
        {
            var totalItems = _caches.Values.Sum(cache => cache.Items.Count);
            var totalMemoryBytes = _caches.Values.Sum(cache => cache.EstimatedMemoryBytes);
            var activePreloadTasks = _preloadTasks.Count(kvp => !kvp.Value.IsCompleted);

            return new CacheStatistics
            {
                TotalCacheItems = totalItems,
                CacheHitCount = _cacheHitCount,
                CacheMissCount = _cacheMissCount,
                MemoryUsageMB = Math.Round(totalMemoryBytes / 1024.0 / 1024.0, 2),
                ActivePreloadTasks = activePreloadTasks,
                LastUpdated = DateTime.Now
            };
        }

        public void ConfigureCache(int maxMemoryMB = 50, int cacheExpirationMinutes = 10, double preloadMultiplier = 2.0)
        {
            _maxMemoryMB = Math.Max(10, maxMemoryMB); // 最少10MB
            _cacheExpirationMinutes = Math.Max(1, cacheExpirationMinutes); // 最少1分钟
            _preloadMultiplier = Math.Max(0.5, Math.Min(5.0, preloadMultiplier)); // 限制在0.5-5.0之间

            Debug.WriteLine($"[DataPreloadService] 缓存配置更新: MaxMemory={_maxMemoryMB}MB, Expiration={_cacheExpirationMinutes}min, PreloadMultiplier={_preloadMultiplier}");
        }

        /// <summary>
        /// 缓存数据范围
        /// </summary>
        private void CacheDataRange<T>(string key, int startIndex, IList<T> data)
        {
            if (data?.Count == 0) return;

            var cache = _caches.GetOrAdd(key, _ => new CacheContainer());
            
            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                if (item == null) continue;

                var itemSize = EstimateObjectSize(item);
                var cacheItem = new CacheContainer.CacheItem
                {
                    Data = item,
                    EstimatedSizeBytes = itemSize
                };

                var itemIndex = startIndex + i;
                cache.Items.AddOrUpdate(
                    itemIndex,
                    cacheItem,
                    (_, existingItem) =>
                    {
                        cache.EstimatedMemoryBytes -= existingItem.EstimatedSizeBytes;
                        return cacheItem;
                    });

                cache.EstimatedMemoryBytes += itemSize;
            }

            cache.LastAccessTime = DateTime.Now;
        }

        /// <summary>
        /// 估算对象内存大小
        /// </summary>
        private long EstimateObjectSize(object obj)
        {
            if (obj == null) return 0;

            // 基础估算，实际实现可能需要更复杂的逻辑
            var type = obj.GetType();
            
            // 基本类型大小
            if (type.IsPrimitive)
            {
                return type == typeof(bool) || type == typeof(byte) ? 1 :
                       type == typeof(short) || type == typeof(ushort) ? 2 :
                       type == typeof(int) || type == typeof(uint) || type == typeof(float) ? 4 :
                       type == typeof(long) || type == typeof(ulong) || type == typeof(double) ? 8 : 4;
            }

            // 字符串
            if (obj is string str)
            {
                return str.Length * 2 + 24; // Unicode字符 + 对象开销
            }

            // 复杂对象粗略估算
            return 200; // 假设平均200字节
        }

        /// <summary>
        /// 管理内存使用
        /// </summary>
        private async Task ManageMemoryUsageAsync()
        {
            await _memoryManagementSemaphore.WaitAsync();
            try
            {
                var stats = GetCacheStatistics();
                if (stats.MemoryUsageMB <= _maxMemoryMB) return;

                Debug.WriteLine($"[DataPreloadService] 内存使用超限: {stats.MemoryUsageMB}MB > {_maxMemoryMB}MB，开始清理");

                // 按最后访问时间排序，清理最久未使用的缓存
                var cachesToRemove = _caches
                    .OrderBy(kvp => kvp.Value.LastAccessTime)
                    .Take(_caches.Count / 3) // 清理1/3的缓存
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in cachesToRemove)
                {
                    _caches.TryRemove(key, out _);
                }

                Debug.WriteLine($"[DataPreloadService] 内存清理完成，清理了 {cachesToRemove.Count} 个缓存");
            }
            finally
            {
                _memoryManagementSemaphore.Release();
            }
        }

        /// <summary>
        /// 定时清理回调
        /// </summary>
        private void CleanupCallback(object? state)
        {
            try
            {
                ClearExpiredCache();
                
                // 清理已完成的预加载任务
                var completedTasks = _preloadTasks
                    .Where(kvp => kvp.Value.IsCompleted)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var taskKey in completedTasks)
                {
                    _preloadTasks.TryRemove(taskKey, out _);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DataPreloadService] 定时清理异常: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _memoryManagementSemaphore?.Dispose();
            _caches.Clear();
            
            // 等待所有预加载任务完成
            var tasks = _preloadTasks.Values.ToArray();
            Task.WhenAll(tasks).ContinueWith(_ => _preloadTasks.Clear());
        }
    }
}