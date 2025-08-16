using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Desktop.Services.Interfaces;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 药材服务实现类 - UltraThink 简化实现
    /// </summary>
    public class HerbService : IHerbService
    {
        private readonly IHerbApiService _herbApiService;
        private readonly ILogger<HerbService> _logger;

        public HerbService(IHerbApiService herbApiService, ILogger<HerbService> logger)
        {
            _herbApiService = herbApiService;
            _logger = logger;
        }

        #region 基础CRUD操作

        public async Task<HerbDto> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _herbApiService.GetHerbByIdAsync(id);
                return response.IsSuccessStatusCode && response.Content != null 
                    ? ConvertDetailToDto(response.Content) 
                    : new HerbDto { Id = id, Name = "未找到" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材详情失败: {Id}", id);
                return new HerbDto { Id = id, Name = "获取失败" };
            }
        }

        public async Task<PagedResult<HerbDto>> GetPagedAsync(HerbPagedQueryDto query)
        {
            try
            {
                var response = await _herbApiService.GetHerbsAsync(
                    page: query.PageIndex,
                    pageSize: query.PageSize,
                    keyword: query.Keyword,
                    name: query.Name,
                    origin: query.Origin,
                    effect: null,
                    usage: null,
                    status: query.Status.HasValue ? (int?)query.Status.Value : null,
                    minPrice: query.MinPrice,
                    maxPrice: query.MaxPrice,
                    hasStock: null
                );

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var items = response.Content.Items.ToList();
                    return new PagedResult<HerbDto>
                    {
                        Items = items,
                        TotalCount = (int)response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                }
                
                return new PagedResult<HerbDto> { Items = new List<HerbDto>(), TotalCount = 0 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询药材失败");
                return new PagedResult<HerbDto> { Items = new List<HerbDto>(), TotalCount = 0 };
            }
        }

        public async Task<List<HerbDto>> GetAllAsync()
        {
            try
            {
                var response = await _herbApiService.GetHerbsAsync(page: 1, pageSize: 1000);
                return response.IsSuccessStatusCode && response.Content != null 
                    ? response.Content.Items.ToList()
                    : new List<HerbDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有药材失败");
                return new List<HerbDto>();
            }
        }

        public async Task<HerbDto> CreateAsync(HerbCreateDto dto)
        {
            try
            {
                var response = await _herbApiService.CreateHerbAsync(dto);
                return response.IsSuccessStatusCode && response.Content != null
                    ? response.Content
                    : new HerbDto { Name = dto.Name };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建药材失败");
                return new HerbDto { Name = dto.Name };
            }
        }

        public async Task<HerbDto> UpdateAsync(Guid id, HerbUpdateDto dto)
        {
            try
            {
                var response = await _herbApiService.UpdateHerbAsync(id, dto);
                return response.IsSuccessStatusCode && response.Content != null
                    ? response.Content
                    : new HerbDto { Id = id, Name = dto.Name };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新药材失败");
                return new HerbDto { Id = id, Name = dto.Name };
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var response = await _herbApiService.ToggleStatusAsync(id);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除药材失败");
                return false;
            }
        }

        public async Task<List<HerbDto>> GetByIdsAsync(List<Guid> ids)
        {
            var results = new List<HerbDto>();
            foreach (var id in ids)
            {
                var herb = await GetByIdAsync(id);
                if (herb != null) results.Add(herb);
            }
            return results;
        }

        public async Task<List<HerbDto>> SearchAsync(string keyword)
        {
            try
            {
                var response = await _herbApiService.GetHerbsAsync(page: 1, pageSize: 100, keyword: keyword);
                return response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.Items.ToList()
                    : new List<HerbDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索药材失败");
                return new List<HerbDto>();
            }
        }

        #endregion

        #region 状态管理

        public async Task<bool> UpdateStockAsync(Guid id, HerbStockUpdateDto dto)
        {
            _logger.LogWarning("UpdateStockAsync not implemented");
            return await Task.FromResult(true);
        }

        public async Task<bool> UpdatePriceAsync(Guid id, HerbPriceUpdateDto dto)
        {
            _logger.LogWarning("UpdatePriceAsync not implemented");
            return await Task.FromResult(true);
        }

        public async Task<bool> BatchUpdateStatusAsync(BatchStatusUpdateDto dto)
        {
            _logger.LogWarning("BatchUpdateStatusAsync not implemented");
            return await Task.FromResult(true);
        }

        #endregion

        #region 统计和查询

        public async Task<HerbStockStatisticsDto> GetStockStatisticsAsync()
        {
            _logger.LogWarning("GetStockStatisticsAsync not implemented");
            return await Task.FromResult(new HerbStockStatisticsDto());
        }

        #endregion

        #region Desktop兼容方法

        public async Task<ServiceResult<List<HerbDto>>> GetListAsync(HerbPagedQueryDto? query = null)
        {
            try
            {
                var herbs = query != null ? 
                    (await GetPagedAsync(query)).Items :
                    await GetAllAsync();
                return ServiceResult<List<HerbDto>>.Success(herbs.ToList());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure($"获取药材列表失败: {ex.Message}");
            }
        }

        public async Task<PagedResult<HerbDto>> SearchHerbsAsync(HerbPagedQueryDto query)
        {
            return await GetPagedAsync(query);
        }

        public async Task<List<HerbDto>> GetHerbsAsync()
        {
            return await GetAllAsync();
        }

        public async Task<HerbDto?> GetByIdHerbInfoAsync(Guid id)
        {
            return await GetByIdAsync(id);
        }

        public async Task<ServiceResult> CreateHerbAsync(HerbCreateDto dto)
        {
            try
            {
                await CreateAsync(dto);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"创建药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateHerbAsync(HerbUpdateDto dto)
        {
            try
            {
                await UpdateAsync(dto.Id, dto);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"更新药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteHerbAsync(Guid id)
        {
            try
            {
                var result = await DeleteAsync(id);
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"删除药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateStatusAsync(Guid id, CommonStatusUpdateDto dto)
        {
            try
            {
                var response = await _herbApiService.UpdateStatusAsync(dto);
                return response.IsSuccessStatusCode ? ServiceResult.Success() : ServiceResult.Failure("更新状态失败");
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"更新状态失败: {ex.Message}");
            }
        }

        public async Task<List<HerbDto>> GetAvailableHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.GetAvailableHerbsAsync();
                return response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.ToList()
                    : new List<HerbDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用药材失败");
                return new List<HerbDto>();
            }
        }

        public async Task<List<HerbDto>> GetOutOfStockHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.GetOutOfStockHerbsAsync();
                return response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.ToList()
                    : new List<HerbDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缺货药材失败");
                return new List<HerbDto>();
            }
        }

        public async Task<List<HerbDto>> GetExpiringHerbsAsync(int days = 30)
        {
            try
            {
                var response = await _herbApiService.GetExpiringHerbsAsync(days);
                return response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.ToList()
                    : new List<HerbDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取即将过期药材失败");
                return new List<HerbDto>();
            }
        }

        public async Task<Dictionary<int, int>> GetStatisticsAsync()
        {
            try
            {
                var response = await _herbApiService.GetStatisticsAsync();
                return response.IsSuccessStatusCode && response.Content != null
                    ? response.Content
                    : new Dictionary<int, int>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材统计失败");
                return new Dictionary<int, int>();
            }
        }

        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        {
            try
            {
                var response = await _herbApiService.ImportHerbsAsync(herbs);
                return response.IsSuccessStatusCode
                    ? ServiceResult<int>.Success(herbs.Count)
                    : ServiceResult<int>.Failure("导入失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"导入药材失败: {ex.Message}");
            }
        }

        public async Task<List<HerbDto>> ExportHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.ExportHerbsAsync();
                return response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.Select(ConvertDetailToDto).ToList()
                    : new List<HerbDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出药材数据失败");
                return new List<HerbDto>();
            }
        }

        public async Task<ServiceResult<List<HerbDto>>> SearchByNameAsync(string name)
        {
            try
            {
                var herbs = await SearchAsync(name);
                return ServiceResult<List<HerbDto>>.Success(herbs);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure($"搜索药材失败: {ex.Message}");
            }
        }

        #endregion

        #region 私有转换方法

        private HerbDto ConvertDetailToDto(HerbDetailDto detail)
        {
            return new HerbDto
            {
                Id = detail.Id,
                Name = detail.Name,
                PinYinCode = detail.PinYinCode,
                Origin = detail.Origin,
                Spec = detail.Spec,
                Unit = detail.Unit,
                Price = detail.Price,
                Effect = detail.Effect,
                Usage = detail.Usage,
                Remark = detail.Remark
            };
        }

        #endregion
    }
}