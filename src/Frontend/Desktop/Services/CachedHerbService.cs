using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Herbs;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Desktop.Core.Caching;
using LYBT.Shared.Models.Common;
using static LYBT.Desktop.Core.Caching.CacheKeyGenerator;
using PagedResult = LYBT.Desktop.Core.Models.Common.PagedResult<LYBT.Desktop.Core.Models.Herbs.HerbInfo>;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 带缓存功能的药材服务装饰器
    /// </summary>
    public class CachedHerbService : IHerbService
    {
        private readonly IHerbService _decoratedService;
        private readonly IMemoryCacheService _cacheService;
        private readonly ILogger<CachedHerbService> _logger;

        // 缓存键前缀（使用标准化生成器）
        private const string CACHE_PREFIX = "herbs";
        private readonly string ALL_HERBS_KEY = Generate<HerbService>("all");
        private readonly string AVAILABLE_HERBS_KEY = Generate<HerbService>("available");

        // 缓存策略配置
        private static readonly CacheOptions DefaultCacheOptions = CacheOptions.MediumTerm; // 30分钟
        private static readonly CacheOptions ShortCacheOptions = CacheOptions.ShortTerm;    // 5分钟
        private static readonly CacheOptions LongCacheOptions = CacheOptions.LongTerm;      // 2小时

        public CachedHerbService(
            IHerbService decoratedService,
            IMemoryCacheService cacheService,
            ILogger<CachedHerbService> logger)
        {
            _decoratedService = decoratedService;
            _cacheService = cacheService;
            _logger = logger;
        }

        #region IHerbService 实现

        /// <summary>
        /// 获取药材列表（带缓存）
        /// </summary>
        public async Task<ApiResult<List<HerbDto>>> GetListAsync(HerbPagedQueryDto? query = null)
        {
            try
            {
                // 为简单查询使用缓存
                if (query == null || IsSimpleQuery(query))
                {
                    var cacheKey = GenerateListCacheKey(query);
                    return await _cacheService.GetAsync(cacheKey, async () =>
                    {
                        _logger.LogDebug("药材列表缓存未命中，调用API获取数据: {CacheKey}", cacheKey);
                        return await _decoratedService.GetListAsync(query);
                    }, DefaultCacheOptions);
                }

                // 复杂查询直接调用服务
                return await _decoratedService.GetListAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材列表失败（带缓存）");
                return ApiResult<List<HerbDto>>.Failure($"获取药材列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分页查询药材（带缓存）
        /// </summary>
        public async Task<PagedResult> SearchHerbsAsync(HerbPagedQueryDto query)
        {
            try
            {
                // 只对特定条件的查询启用缓存
                if (ShouldCacheQuery(query))
                {
                    var cacheKey = GenerateSearchCacheKey(query);
                    return await _cacheService.GetAsync(cacheKey, async () =>
                    {
                        _logger.LogDebug("药材搜索缓存未命中，调用API获取数据: {CacheKey}", cacheKey);
                        return await _decoratedService.SearchHerbsAsync(query);
                    }, ShortCacheOptions);
                }

                return await _decoratedService.SearchHerbsAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索药材失败（带缓存）");
                return new PagedResult { Items = new List<HerbInfo>(), TotalCount = 0 };
            }
        }

        /// <summary>
        /// 获取可用药材（重点缓存）
        /// </summary>
        public async Task<List<HerbInfo>> GetAvailableHerbsAsync()
        {
            try
            {
                return await _cacheService.GetAsync(AVAILABLE_HERBS_KEY, async () =>
                {
                    _logger.LogDebug("可用药材缓存未命中，调用API获取数据");
                    return await _decoratedService.GetAvailableHerbsAsync();
                }, LongCacheOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用药材失败（带缓存）");
                return new List<HerbInfo>();
            }
        }


        /// <summary>
        /// 获取药材列表（带缓存）
        /// </summary>
        public async Task<List<HerbInfo>> GetHerbsAsync()
        {
            try
            {
                return await _cacheService.GetAsync(ALL_HERBS_KEY, async () =>
                {
                    _logger.LogDebug("药材列表缓存未命中，调用API获取数据");
                    return await _decoratedService.GetHerbsAsync();
                }, LongCacheOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材列表失败（带缓存）");
                return new List<HerbInfo>();
            }
        }

        /// <summary>
        /// 获取药材详情（通过ID）
        /// </summary>
        public async Task<HerbInfo?> GetByIdAsync(Guid id)
        {
            try
            {
                var cacheKey = Generate<HerbService>("detail", id);
                return await _cacheService.GetAsync(cacheKey, async () =>
                {
                    _logger.LogDebug("药材详情缓存未命中，调用API获取数据: {HerbId}", id);
                    return await _decoratedService.GetByIdAsync(id);
                }, LongCacheOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材详情失败（带缓存）: {HerbId}", id);
                return null;
            }
        }

        /// <summary>
        /// 创建药材
        /// </summary>
        public async Task<ServiceResult> CreateHerbAsync(HerbCreateDto dto)
        {
            var result = await _decoratedService.CreateHerbAsync(dto);
            if (result.IsSuccess)
            {
                ClearCache();
            }
            return result;
        }

        /// <summary>
        /// 更新药材
        /// </summary>
        public async Task<ServiceResult> UpdateHerbAsync(HerbUpdateDto dto)
        {
            var result = await _decoratedService.UpdateHerbAsync(dto);
            if (result.IsSuccess)
            {
                ClearHerbCache(dto.Id);
                ClearCache();
            }
            return result;
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        public async Task<ServiceResult> DeleteHerbAsync(Guid id)
        {
            var result = await _decoratedService.DeleteHerbAsync(id);
            if (result.IsSuccess)
            {
                ClearHerbCache(id);
                ClearCache();
            }
            return result;
        }

        /// <summary>
        /// 更新药材状态
        /// </summary>
        public async Task<ServiceResult> UpdateStatusAsync(Guid id, CommonStatusUpdateDto dto)
        {
            var result = await _decoratedService.UpdateStatusAsync(id, dto);
            if (result.IsSuccess)
            {
                ClearHerbCache(id);
                ClearCache();
            }
            return result;
        }

        /// <summary>
        /// 获取缺货药材列表（带缓存）
        /// </summary>
        public async Task<List<HerbInfo>> GetOutOfStockHerbsAsync()
        {
            try
            {
                var cacheKey = Generate<HerbService>("outofstock");
                return await _cacheService.GetAsync(cacheKey, async () =>
                {
                    _logger.LogDebug("缺货药材缓存未命中，调用API获取数据");
                    return await _decoratedService.GetOutOfStockHerbsAsync();
                }, ShortCacheOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缺货药材失败（带缓存）");
                return new List<HerbInfo>();
            }
        }

        /// <summary>
        /// 获取即将过期的药材（带缓存）
        /// </summary>
        public async Task<List<HerbInfo>> GetExpiringHerbsAsync(int days = 30)
        {
            try
            {
                var cacheKey = Generate<HerbService>("expiring", days);
                return await _cacheService.GetAsync(cacheKey, async () =>
                {
                    _logger.LogDebug("即将过期药材缓存未命中，调用API获取数据");
                    return await _decoratedService.GetExpiringHerbsAsync(days);
                }, ShortCacheOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取即将过期药材失败（带缓存）");
                return new List<HerbInfo>();
            }
        }

        /// <summary>
        /// 获取药材统计信息（带缓存）
        /// </summary>
        public async Task<Dictionary<int, int>> GetStatisticsAsync()
        {
            try
            {
                var cacheKey = Generate<HerbService>("statistics", "status");
                return await _cacheService.GetAsync(cacheKey, async () =>
                {
                    _logger.LogDebug("药材统计信息缓存未命中，调用API获取数据");
                    return await _decoratedService.GetStatisticsAsync();
                }, DefaultCacheOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材统计信息失败（带缓存）");
                return new Dictionary<int, int>();
            }
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        {
            var result = await _decoratedService.ImportHerbsAsync(herbs);
            if (result.IsSuccess)
            {
                ClearCache();
            }
            return result;
        }

        /// <summary>
        /// 导出药材数据
        /// </summary>
        public async Task<List<HerbInfo>> ExportHerbsAsync()
        {
            return await _decoratedService.ExportHerbsAsync();
        }

        /// <summary>
        /// 按名称搜索药材（带缓存）
        /// </summary>
        public async Task<ServiceResult<List<HerbInfo>>> SearchByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return ServiceResult<List<HerbInfo>>.Success(new List<HerbInfo>());
            }

            try
            {
                var cacheKey = Generate<HerbService>("search", "name", name);
                var result = await _cacheService.GetAsync(cacheKey, async () =>
                {
                    _logger.LogDebug("按名称搜索药材缓存未命中，调用API获取数据: {Name}", name);
                    return await _decoratedService.SearchByNameAsync(name);
                }, ShortCacheOptions);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "按名称搜索药材失败（带缓存）: {Name}", name);
                return ServiceResult<List<HerbInfo>>.Failure($"搜索药材失败: {ex.Message}");
            }
        }


        #endregion

        #region 缓存管理方法

        /// <summary>
        /// 清除所有药材相关缓存
        /// </summary>
        public void ClearCache()
        {
            try
            {
                _cacheService.ClearByPrefix(CACHE_PREFIX);
                _logger.LogInformation("已清除所有药材缓存");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除药材缓存失败");
            }
        }

        /// <summary>
        /// 清除特定药材的缓存
        /// </summary>
        public void ClearHerbCache(Guid herbId)
        {
            try
            {
                var cacheKey = Generate<HerbService>("detail", herbId);
                _cacheService.Remove(cacheKey);
                _logger.LogDebug("已清除药材详情缓存: {HerbId}", herbId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除药材缓存失败: {HerbId}", herbId);
            }
        }

        /// <summary>
        /// 刷新可用药材缓存
        /// </summary>
        public async Task RefreshAvailableHerbsAsync()
        {
            try
            {
                _cacheService.Remove(AVAILABLE_HERBS_KEY);
                await GetAvailableHerbsAsync(); // 重新加载缓存
                _logger.LogInformation("已刷新可用药材缓存");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新可用药材缓存失败");
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public CacheStatistics GetCacheStatistics()
        {
            return _cacheService.GetStatistics();
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 生成列表查询的缓存键（使用标准化缓存键生成器）
        /// </summary>
        private string GenerateListCacheKey(HerbPagedQueryDto? query)
        {
            if (query == null)
                return Generate<HerbService>("list", "all");

            var parameters = new List<object> { "list" };
            
            if (!string.IsNullOrEmpty(query.Keyword))
                parameters.Add($"kw:{query.Keyword}");
            if (!string.IsNullOrEmpty(query.Name))
                parameters.Add($"name:{query.Name}");
            if (!string.IsNullOrEmpty(query.Origin))
                parameters.Add($"origin:{query.Origin}");
            if (query.Status.HasValue)
                parameters.Add($"status:{query.Status.Value}");
            if (query.MinPrice.HasValue)
                parameters.Add($"minPrice:{query.MinPrice.Value}");
            if (query.MaxPrice.HasValue)
                parameters.Add($"maxPrice:{query.MaxPrice.Value}");

            return Generate<HerbService>("list", parameters.ToArray());
        }

        /// <summary>
        /// 生成搜索查询的缓存键（使用标准化缓存键生成器）
        /// </summary>
        private string GenerateSearchCacheKey(HerbPagedQueryDto query)
        {
            var parameters = new List<object>
            {
                "search",
                $"page:{query.PageIndex}",
                $"size:{query.PageSize}"
            };
            
            if (!string.IsNullOrEmpty(query.Keyword))
                parameters.Add($"kw:{query.Keyword}");
            if (!string.IsNullOrEmpty(query.Name))
                parameters.Add($"name:{query.Name}");
            if (query.Status.HasValue)
                parameters.Add($"status:{query.Status.Value}");
            if (query.MinPrice.HasValue)
                parameters.Add($"minPrice:{query.MinPrice.Value}");
            if (query.MaxPrice.HasValue)
                parameters.Add($"maxPrice:{query.MaxPrice.Value}");

            return Generate<HerbService>("search", parameters.ToArray());
        }

        /// <summary>
        /// 判断是否为简单查询（可以缓存）
        /// </summary>
        private bool IsSimpleQuery(HerbPagedQueryDto query)
        {
            return query.MinPrice == null && 
                   query.MaxPrice == null && 
                   string.IsNullOrEmpty(query.Keyword);
        }

        /// <summary>
        /// 判断查询是否应该被缓存
        /// </summary>
        private bool ShouldCacheQuery(HerbPagedQueryDto query)
        {
            // 只缓存前几页的简单查询
            return query.PageIndex <= 3 && 
                   query.PageSize <= 50 && 
                   IsSimpleQuery(query);
        }

        #endregion
    }
}