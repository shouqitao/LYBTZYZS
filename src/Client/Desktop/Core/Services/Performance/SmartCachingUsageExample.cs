using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Services.Performance;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// 智能缓存使用示例
    /// 展示如何在 ViewModel 中集成智能预加载服务
    /// </summary>
    public class SmartCachingUsageExample
    {
        private readonly IDataPreloadService _preloadService;
        private readonly LYBT.Shared.Interfaces.Services.IHerbService _herbService; // 假设存在的药材服务

        public SmartCachingUsageExample(IDataPreloadService preloadService, LYBT.Shared.Interfaces.Services.IHerbService herbService)
        {
            _preloadService = preloadService ?? throw new ArgumentNullException(nameof(preloadService));
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));

            // 配置缓存参数 - 针对药材数据优化
            _preloadService.ConfigureCache(
                maxMemoryMB: 80,        // 药材数据相对较大，分配更多内存
                cacheExpirationMinutes: 20,  // 药材信息变化不频繁，可以缓存更久
                preloadMultiplier: 1.8  // 适中的预加载倍数
            );
        }

        /// <summary>
        /// 智能加载药材列表 - 集成预加载策略
        /// </summary>
        public async Task<IList<HerbDto>> SmartLoadHerbsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            var cacheKey = "herb_list";
            var startIndex = pageIndex * pageSize;

            // 1. 首先尝试从缓存获取数据
            var cachedData = _preloadService.GetCachedRange<HerbDto>(cacheKey, startIndex, pageSize);
            if (cachedData.Count == pageSize)
            {
                // 缓存命中，直接返回
                return cachedData;
            }

            // 2. 缓存未命中，从服务器加载数据
            var serverObjects = await LoadHerbsFromServer(startIndex, pageSize, cancellationToken);
            var serverData = serverObjects.Cast<HerbDto>().ToList();

            // 3. 启动智能预加载 - 预测用户接下来可能查看的数据
            await StartIntelligentPreloadAsync(cacheKey, startIndex, pageSize, cancellationToken);

            return serverData;
        }

        /// <summary>
        /// 智能搜索药材 - 带预加载优化
        /// </summary>
        public async Task<IList<HerbDto>> SmartSearchHerbsAsync(
            string keyword, 
            int pageIndex, 
            int pageSize, 
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"herb_search_{keyword?.ToLowerInvariant()}";
            var startIndex = pageIndex * pageSize;

            // 搜索结果缓存策略
            var cachedResults = _preloadService.GetCachedRange<HerbDto>(cacheKey, startIndex, pageSize);
            if (cachedResults.Count == pageSize)
            {
                return cachedResults;
            }

            // 执行搜索并缓存结果
            var searchResults = await SearchHerbsFromServer(keyword ?? string.Empty, startIndex, pageSize, cancellationToken);
            
            // 为搜索结果启动预加载
            _ = Task.Run(async () =>
            {
                await _preloadService.PreloadDataAsync(
                    cacheKey, 
                    startIndex + pageSize, 
                    pageSize * 2, // 搜索场景下预加载更多
                    (start, count, token) => SearchDataProvider(keyword ?? string.Empty, start, count, token),
                    cancellationToken);
            }, cancellationToken);

            return searchResults;
        }

        /// <summary>
        /// 启动智能预加载策略
        /// </summary>
        private async Task StartIntelligentPreloadAsync(string cacheKey, int currentIndex, int pageSize, CancellationToken cancellationToken)
        {
            // 不阻塞当前操作，异步执行预加载
            _ = Task.Run(async () =>
            {
                try
                {
                    // 预加载策略 1: 预加载下一页
                    var nextPageStart = currentIndex + pageSize;
                    await _preloadService.PreloadDataAsync(
                        cacheKey,
                        nextPageStart,
                        pageSize,
                        LoadHerbsFromServer,
                        cancellationToken);

                    // 预加载策略 2: 如果用户在第一页，预加载前几页的数据（常见浏览模式）
                    if (currentIndex == 0)
                    {
                        for (int i = 1; i <= 3; i++) // 预加载第2-4页
                        {
                            await _preloadService.PreloadDataAsync(
                                cacheKey,
                                i * pageSize,
                                pageSize,
                                LoadHerbsFromServer,
                                cancellationToken);
                            
                            // 避免过于激进的预加载
                            await Task.Delay(100, cancellationToken);
                        }
                    }
                    // 预加载策略 3: 中间页面，预加载前后页面
                    else if (currentIndex > 0)
                    {
                        // 预加载上一页
                        var prevPageStart = Math.Max(0, currentIndex - pageSize);
                        await _preloadService.PreloadDataAsync(
                            cacheKey,
                            prevPageStart,
                            pageSize,
                            LoadHerbsFromServer,
                            cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // 正常取消，不需要处理
                }
                catch (Exception ex)
                {
                    // 预加载失败不应影响主流程，记录日志即可
                    System.Diagnostics.Debug.WriteLine($"[SmartCaching] 预加载失败: {ex.Message}");
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 从服务器加载药材数据 - 供预加载服务调用
        /// </summary>
        private async Task<IList<object>> LoadHerbsFromServer(int startIndex, int count, CancellationToken cancellationToken)
        {
            var pageIndex = (startIndex / count) + 1; // 修正为1基索引
            var query = new HerbPagedQueryDto
            {
                PageIndex = pageIndex,
                PageSize = count
            };
            
            var result = await _herbService.GetPagedAsync(query);
            if (result.IsSuccess && result.Data != null)
            {
                return result.Data.Items.Cast<object>().ToList();
            }
            return new List<object>();
        }

        /// <summary>
        /// 搜索数据提供器 - 供预加载服务调用
        /// </summary>
        private async Task<IList<object>> SearchDataProvider(string keyword, int startIndex, int count, CancellationToken cancellationToken)
        {
            var result = await _herbService.SearchAsync(keyword);
            if (result.IsSuccess && result.Data != null)
            {
                // 手动实现分页逻辑，因为SearchAsync不支持分页
                var pagedResults = result.Data.Skip(startIndex).Take(count).ToList();
                return pagedResults.Cast<object>().ToList();
            }
            return new List<object>();
        }

        /// <summary>
        /// 从服务器搜索药材
        /// </summary>
        private async Task<IList<HerbDto>> SearchHerbsFromServer(string keyword, int startIndex, int count, CancellationToken cancellationToken)
        {
            var result = await _herbService.SearchAsync(keyword);
            if (result.IsSuccess && result.Data != null)
            {
                // 手动实现分页逻辑，因为SearchAsync不支持分页
                return result.Data.Skip(startIndex).Take(count).ToList();
            }
            return new List<HerbDto>();
        }

        /// <summary>
        /// 获取缓存性能报告
        /// </summary>
        public string GetCachePerformanceReport()
        {
            var stats = _preloadService.GetCacheStatistics();
            
            return $"药材数据缓存性能报告:\n" +
                   $"- 缓存项数: {stats.TotalCacheItems}\n" +
                   $"- 命中次数: {stats.CacheHitCount}\n" +
                   $"- 未命中次数: {stats.CacheMissCount}\n" +
                   $"- 命中率: {stats.HitRatio:P2}\n" +
                   $"- 内存使用: {stats.MemoryUsageMB:F1} MB\n" +
                   $"- 活跃预加载任务: {stats.ActivePreloadTasks}\n" +
                   $"- 最后更新: {stats.LastUpdated:HH:mm:ss}";
        }

        /// <summary>
        /// 优化建议 - 根据缓存统计动态调整策略
        /// </summary>
        public void OptimizeCachingStrategy()
        {
            var stats = _preloadService.GetCacheStatistics();
            
            // 根据命中率调整预加载策略
            if (stats.HitRatio < 0.3) // 命中率低于30%
            {
                // 减少预加载，避免浪费资源
                _preloadService.ConfigureCache(
                    maxMemoryMB: 50,
                    cacheExpirationMinutes: 10,
                    preloadMultiplier: 1.2);
                    
                System.Diagnostics.Debug.WriteLine("[SmartCaching] 命中率较低，减少预加载强度");
            }
            else if (stats.HitRatio > 0.8) // 命中率高于80%
            {
                // 增加预加载，提升用户体验
                _preloadService.ConfigureCache(
                    maxMemoryMB: 100,
                    cacheExpirationMinutes: 25,
                    preloadMultiplier: 2.5);
                    
                System.Diagnostics.Debug.WriteLine("[SmartCaching] 命中率较高，增加预加载强度");
            }
        }

        /// <summary>
        /// 清理策略 - 在适当时机清理缓存
        /// </summary>
        public void CleanupCacheStrategically()
        {
            var stats = _preloadService.GetCacheStatistics();
            
            // 内存使用过多时清理
            if (stats.MemoryUsageMB > 150)
            {
                _preloadService.ClearExpiredCache();
                System.Diagnostics.Debug.WriteLine("[SmartCaching] 内存使用过多，执行缓存清理");
            }
            
            // 在用户退出药材管理模块时清理
            // 这通常在 ViewModel 的 OnNavigatedFrom 或类似方法中调用
        }
    }

    // 注释：HerbDto类定义已迁移到Shared层，统一使用LYBT.Shared.Models.Contracts.Herbs.HerbDto
    // 注释：IHerbService接口定义已迁移到Shared层，统一使用LYBT.Shared.Interfaces.Services.IHerbService
}