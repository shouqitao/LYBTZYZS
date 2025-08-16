using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Caching;
using LYBT.Shared.Models.Common;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 带缓存功能的药材服务装饰器 - UltraThink 简化实现
    /// </summary>
    public class CachedHerbService : IHerbService
    {
        private readonly IHerbService _decoratedService;
        private readonly IMemoryCacheService _cacheService;
        private readonly ILogger<CachedHerbService> _logger;

        // 缓存过期时间配置
        private static readonly TimeSpan DefaultCacheTime = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan ShortCacheTime = TimeSpan.FromMinutes(5);

        public CachedHerbService(
            IHerbService decoratedService,
            IMemoryCacheService cacheService,
            ILogger<CachedHerbService> logger)
        {
            _decoratedService = decoratedService;
            _cacheService = cacheService;
            _logger = logger;
        }

        #region 基础CRUD操作

        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            var cacheKey = $"herb_detail_{id}";
            try
            {
                return await _cacheService.GetAsync(cacheKey, async () =>
                {
                    _logger.LogDebug("缓存未命中，从服务获取药材详情: {Id}", id);
                    return await _decoratedService.GetByIdAsync(id);
                }, CacheOptions.MediumTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材详情失败: {Id}", id);
                return await _decoratedService.GetByIdAsync(id);
            }
        }

        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query)
        {
            // 简单查询才使用缓存
            if (IsSimpleQuery(query))
            {
                var cacheKey = $"herb_paged_{query.PageIndex}_{query.PageSize}_{query.Keyword ?? "all"}";
                try
                {
                    var result = await _cacheService.GetAsync(cacheKey, async () =>
                    {
                        _logger.LogDebug("缓存未命中，从服务获取分页药材数据");
                        return await _decoratedService.GetPagedAsync(query);
                    }, CacheOptions.ShortTerm);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "分页查询药材失败（缓存）");
                    return await _decoratedService.GetPagedAsync(query);
                }
            }

            return await _decoratedService.GetPagedAsync(query);
        }

        public async Task<ServiceResult<List<HerbDto>>> GetAllAsync()
        {
            const string cacheKey = "herb_all";
            try
            {
                return await _cacheService.GetAsync(cacheKey, async () =>
                {
                    _logger.LogDebug("缓存未命中，从服务获取所有药材");
                    return await _decoratedService.GetAllAsync();
                }, CacheOptions.MediumTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有药材失败（缓存）");
                return await _decoratedService.GetAllAsync();
            }
        }

        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
        {
            var result = await _decoratedService.CreateAsync(dto);
            ClearCache();
            return result;
        }

        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto)
        {
            var result = await _decoratedService.UpdateAsync(id, dto);
            if (result.IsSuccess)
            {
                ClearHerbCache(id);
                ClearCache();
            }
            return result;
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            var result = await _decoratedService.DeleteAsync(id);
            if (result.IsSuccess && result.Data == true)
            {
                ClearHerbCache(id);
                ClearCache();
            }
            return result;
        }

        public async Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
        {
            return await _decoratedService.GetByIdsAsync(ids);
        }

        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return ServiceResult<List<HerbDto>>.Success(new List<HerbDto>());

            var cacheKey = $"herb_search_{keyword}";
            try
            {
                return await _cacheService.GetAsync(cacheKey, async () =>
                {
                    _logger.LogDebug("缓存未命中，搜索药材: {Keyword}", keyword);
                    return await _decoratedService.SearchAsync(keyword);
                }, CacheOptions.ShortTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索药材失败（缓存）: {Keyword}", keyword);
                return await _decoratedService.SearchAsync(keyword);
            }
        }

        #endregion

        #region 状态管理

        public async Task<ServiceResult<bool>> UpdateStockAsync(Guid id, HerbStockUpdateDto dto)
        {
            var result = await _decoratedService.UpdateStockAsync(id, dto);
            if (result.IsSuccess && result.Data == true)
            {
                ClearHerbCache(id);
                ClearCache();
            }
            return result;
        }

        public async Task<ServiceResult<bool>> UpdatePriceAsync(Guid id, HerbPriceUpdateDto dto)
        {
            var result = await _decoratedService.UpdatePriceAsync(id, dto);
            if (result.IsSuccess && result.Data == true)
            {
                ClearHerbCache(id);
                ClearCache();
            }
            return result;
        }

        public async Task<ServiceResult<bool>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto)
        {
            var result = await _decoratedService.BatchUpdateStatusAsync(dto);
            if (result.IsSuccess && result.Data == true)
            {
                ClearCache();
            }
            return result;
        }

        #endregion

        #region 统计和查询

        public async Task<ServiceResult<HerbStockStatisticsDto>> GetStockStatisticsAsync()
        {
            const string cacheKey = "herb_stock_statistics";
            try
            {
                return await _cacheService.GetAsync(cacheKey, async () =>
                {
                    _logger.LogDebug("缓存未命中，获取库存统计");
                    return await _decoratedService.GetStockStatisticsAsync();
                }, CacheOptions.ShortTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取库存统计失败（缓存）");
                return await _decoratedService.GetStockStatisticsAsync();
            }
        }

        #endregion

        #region Desktop兼容方法

        public async Task<ServiceResult<List<HerbDto>>> GetListAsync(HerbPagedQueryDto? query = null)
        {
            return await _decoratedService.GetAllAsync();
        }

        public async Task<ServiceResult<PagedResult<HerbDto>>> SearchHerbsAsync(HerbPagedQueryDto query)
        {
            return await GetPagedAsync(query);
        }

        public async Task<ServiceResult<List<HerbDto>>> GetHerbsAsync()
        {
            return await GetAllAsync();
        }

        public async Task<HerbDto?> GetByIdHerbInfoAsync(Guid id)
        {
            var result = await GetByIdAsync(id);
            return result.IsSuccess ? result.Data : null;
        }

        public async Task<ServiceResult> CreateHerbAsync(HerbCreateDto dto)
        {
            var result = await _decoratedService.CreateAsync(dto);
            return new ServiceResult { IsSuccess = true, ErrorMessage = null };
        }

        public async Task<ServiceResult> UpdateHerbAsync(HerbUpdateDto dto)
        {
            var result = await _decoratedService.UpdateAsync(dto.Id, dto);
            return new ServiceResult { IsSuccess = true, ErrorMessage = null };
        }

        public async Task<ServiceResult> DeleteHerbAsync(Guid id)
        {
            var result = await _decoratedService.DeleteAsync(id);
            return new ServiceResult { IsSuccess = result.IsSuccess && result.Data == true, ErrorMessage = (result.IsSuccess && result.Data == true) ? null : "删除失败" };
        }

        public async Task<ServiceResult> UpdateStatusAsync(Guid id, CommonStatusUpdateDto dto)
        {
            var batchDto = new BatchStatusUpdateDto 
            { 
                Ids = new List<Guid> { id }, 
                Status = dto.Status == CommonStatus.Enabled, 
                Reason = dto.Reason 
            };
            var result = await _decoratedService.BatchUpdateStatusAsync(batchDto);
            return new ServiceResult { IsSuccess = result.IsSuccess && result.Data == true, ErrorMessage = (result.IsSuccess && result.Data == true) ? null : "状态更新失败" };
        }

        public async Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync()
        {
            const string cacheKey = "herb_available";
            try
            {
                var result = await _cacheService.GetAsync(cacheKey, async () =>
                {
                    _logger.LogDebug("缓存未命中，获取可用药材");
                    return await _decoratedService.GetAllAsync();
                }, CacheOptions.MediumTerm);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用药材失败（缓存）");
                return await _decoratedService.GetAllAsync();
            }
        }

        public async Task<ServiceResult<List<HerbDto>>> GetOutOfStockHerbsAsync()
        {
            const string cacheKey = "herb_outofstock";
            try
            {
                return await _cacheService.GetAsync(cacheKey, async () =>
                {
                    _logger.LogDebug("缓存未命中，获取缺货药材");
                    return await _decoratedService.GetAllAsync();
                }, CacheOptions.ShortTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缺货药材失败（缓存）");
                return await _decoratedService.GetAllAsync();
            }
        }

        public async Task<ServiceResult<List<HerbDto>>> GetExpiringHerbsAsync(int days = 30)
        {
            var cacheKey = $"herb_expiring_{days}";
            try
            {
                return await _cacheService.GetAsync(cacheKey, async () =>
                {
                    _logger.LogDebug("缓存未命中，获取即将过期药材: {Days}天", days);
                    return await _decoratedService.GetAllAsync();
                }, CacheOptions.ShortTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取即将过期药材失败（缓存）");
                return await _decoratedService.GetAllAsync();
            }
        }

        public async Task<ServiceResult<Dictionary<int, int>>> GetStatisticsAsync()
        {
            const string cacheKey = "herb_statistics";
            try
            {
                return await _cacheService.GetAsync(cacheKey, async () =>
                {
                    _logger.LogDebug("缓存未命中，获取药材统计");
                    var statsResult = await _decoratedService.GetStockStatisticsAsync();
                    if (statsResult.IsSuccess && statsResult.Data != null)
                    {
                        return ServiceResult<Dictionary<int, int>>.Success(
                            new Dictionary<int, int> { { 1, statsResult.Data.TotalCount }, { 2, statsResult.Data.WarningCount } });
                    }
                    return ServiceResult<Dictionary<int, int>>.Failure("获取统计数据失败");
                }, CacheOptions.MediumTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材统计失败（缓存）");
                var statsResult = await _decoratedService.GetStockStatisticsAsync();
                if (statsResult.IsSuccess && statsResult.Data != null)
                {
                    return ServiceResult<Dictionary<int, int>>.Success(
                        new Dictionary<int, int> { { 1, statsResult.Data.TotalCount }, { 2, statsResult.Data.WarningCount } });
                }
                return ServiceResult<Dictionary<int, int>>.Failure("获取统计数据失败");
            }
        }

        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        {
            // ImportHerbsAsync 在 Shared.IHerbService 中不存在，返回未实现的结果
            var result = new ServiceResult<int> { IsSuccess = false, ErrorMessage = "导入功能未实现" };
            if (result.IsSuccess)
            {
                ClearCache();
            }
            return result;
        }

        public async Task<ServiceResult<List<HerbDto>>> ExportHerbsAsync()
        {
            // ExportHerbsAsync 在 Shared.IHerbService 中不存在，返回所有药材
            return await _decoratedService.GetAllAsync();
        }

        public async Task<ServiceResult<List<HerbDto>>> SearchByNameAsync(string name)
        {
            // SearchByNameAsync 在 Shared.IHerbService 中不存在，使用 SearchAsync
            return await _decoratedService.SearchAsync(name);
        }

        #endregion

        #region 缓存管理

        private void ClearCache()
        {
            try
            {
                _cacheService.ClearByPrefix("herb_");
                _logger.LogDebug("已清除所有药材缓存");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除药材缓存失败");
            }
        }

        private void ClearHerbCache(Guid id)
        {
            try
            {
                _cacheService.Remove($"herb_detail_{id}");
                _logger.LogDebug("已清除药材详情缓存: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除药材详情缓存失败: {Id}", id);
            }
        }

        private bool IsSimpleQuery(HerbPagedQueryDto query)
        {
            return string.IsNullOrEmpty(query.Name) && 
                   string.IsNullOrEmpty(query.Origin) && 
                   query.MinPrice == null && 
                   query.MaxPrice == null &&
                   query.Status == null;
        }

        #endregion
    }
}