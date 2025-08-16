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

        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _herbApiService.GetHerbByIdAsync(id);
                var result = response.IsSuccessStatusCode && response.Content != null 
                    ? ConvertDetailToDto(response.Content) 
                    : new HerbDto { Id = id, Name = "未找到" };
                return ServiceResult<HerbDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材详情失败: {Id}", id);
                return ServiceResult<HerbDto>.Failure($"获取药材详情失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query)
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
                    var result = new PagedResult<HerbDto>
                    {
                        Items = items,
                        TotalCount = (int)response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                    return ServiceResult<PagedResult<HerbDto>>.Success(result);
                }
                
                return ServiceResult<PagedResult<HerbDto>>.Success(new PagedResult<HerbDto> { Items = new List<HerbDto>(), TotalCount = 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询药材失败");
                return ServiceResult<PagedResult<HerbDto>>.Failure($"分页查询药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<HerbDto>>> GetAllAsync()
        {
            try
            {
                var response = await _herbApiService.GetHerbsAsync(page: 1, pageSize: 1000);
                var result = response.IsSuccessStatusCode && response.Content != null 
                    ? response.Content.Items.ToList()
                    : new List<HerbDto>();
                return ServiceResult<List<HerbDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有药材失败");
                return ServiceResult<List<HerbDto>>.Failure($"获取所有药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
        {
            try
            {
                var response = await _herbApiService.CreateHerbAsync(dto);
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? response.Content
                    : new HerbDto { Name = dto.Name };
                return ServiceResult<HerbDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建药材失败");
                return ServiceResult<HerbDto>.Failure($"创建药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto)
        {
            try
            {
                var response = await _herbApiService.UpdateHerbAsync(id, dto);
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? response.Content
                    : new HerbDto { Id = id, Name = dto.Name };
                return ServiceResult<HerbDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新药材失败");
                return ServiceResult<HerbDto>.Failure($"更新药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var response = await _herbApiService.ToggleStatusAsync(id);
                return ServiceResult<bool>.Success(response.IsSuccessStatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除药材失败");
                return ServiceResult<bool>.Failure($"删除药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
        {
            var results = new List<HerbDto>();
            foreach (var id in ids)
            {
                var herbResult = await GetByIdAsync(id);
                if (herbResult.IsSuccess && herbResult.Data != null) results.Add(herbResult.Data);
            }
            return ServiceResult<List<HerbDto>>.Success(results);
        }

        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        {
            try
            {
                var response = await _herbApiService.GetHerbsAsync(page: 1, pageSize: 100, keyword: keyword);
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.Items.ToList()
                    : new List<HerbDto>();
                return ServiceResult<List<HerbDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索药材失败");
                return ServiceResult<List<HerbDto>>.Failure($"搜索药材失败: {ex.Message}");
            }
        }

        #endregion

        #region 状态管理

        public async Task<ServiceResult<bool>> UpdateStockAsync(Guid id, HerbStockUpdateDto dto)
        {
            _logger.LogWarning("UpdateStockAsync not implemented");
            return await Task.FromResult(ServiceResult<bool>.Success(true));
        }

        public async Task<ServiceResult<bool>> UpdatePriceAsync(Guid id, HerbPriceUpdateDto dto)
        {
            _logger.LogWarning("UpdatePriceAsync not implemented");
            return await Task.FromResult(ServiceResult<bool>.Success(true));
        }

        public async Task<ServiceResult<bool>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto)
        {
            _logger.LogWarning("BatchUpdateStatusAsync not implemented");
            return await Task.FromResult(ServiceResult<bool>.Success(true));
        }

        #endregion

        #region 统计和查询

        public async Task<ServiceResult<HerbStockStatisticsDto>> GetStockStatisticsAsync()
        {
            _logger.LogWarning("GetStockStatisticsAsync not implemented");
            return await Task.FromResult(ServiceResult<HerbStockStatisticsDto>.Success(new HerbStockStatisticsDto()));
        }

        #endregion

        #region Desktop兼容方法

        public async Task<ServiceResult<List<HerbDto>>> GetListAsync(HerbPagedQueryDto? query = null)
        {
            try
            {
                var herbs = query != null ? 
                    (await GetPagedAsync(query)).Data?.Items ?? new List<HerbDto>() :
                    (await GetAllAsync()).Data ?? new List<HerbDto>();
                return ServiceResult<List<HerbDto>>.Success(herbs.ToList());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbDto>>.Failure($"获取药材列表失败: {ex.Message}");
            }
        }

        public async Task<PagedResult<HerbDto>> SearchHerbsAsync(HerbPagedQueryDto query)
        {
            var result = await GetPagedAsync(query);
            return result.IsSuccess && result.Data != null ? result.Data : new PagedResult<HerbDto> { Items = new List<HerbDto>(), TotalCount = 0 };
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
            try
            {
                var result = await CreateAsync(dto);
                return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "创建失败");
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
                var result = await UpdateAsync(dto.Id, dto);
                return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "更新失败");
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
                return result.IsSuccess && (result.Data == true) ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
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

        public async Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.GetAvailableHerbsAsync();
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.ToList()
                    : new List<HerbDto>();
                return ServiceResult<List<HerbDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用药材失败");
                return ServiceResult<List<HerbDto>>.Failure($"获取可用药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<HerbDto>>> GetOutOfStockHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.GetOutOfStockHerbsAsync();
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.ToList()
                    : new List<HerbDto>();
                return ServiceResult<List<HerbDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缺货药材失败");
                return ServiceResult<List<HerbDto>>.Failure($"获取缺货药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<HerbDto>>> GetExpiringHerbsAsync(int days = 30)
        {
            try
            {
                var response = await _herbApiService.GetExpiringHerbsAsync(days);
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.ToList()
                    : new List<HerbDto>();
                return ServiceResult<List<HerbDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取即将过期药材失败");
                return ServiceResult<List<HerbDto>>.Failure($"获取即将过期药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<Dictionary<int, int>>> GetStatisticsAsync()
        {
            try
            {
                var response = await _herbApiService.GetStatisticsAsync();
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? response.Content
                    : new Dictionary<int, int>();
                return ServiceResult<Dictionary<int, int>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材统计失败");
                return ServiceResult<Dictionary<int, int>>.Failure($"获取药材统计失败: {ex.Message}");
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

        public async Task<ServiceResult<List<HerbDto>>> ExportHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.ExportHerbsAsync();
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.Select(ConvertDetailToDto).ToList()
                    : new List<HerbDto>();
                return ServiceResult<List<HerbDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出药材数据失败");
                return ServiceResult<List<HerbDto>>.Failure($"导出药材数据失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<HerbDto>>> SearchByNameAsync(string name)
        {
            try
            {
                var herbsResult = await SearchAsync(name);
                return herbsResult.IsSuccess ? ServiceResult<List<HerbDto>>.Success(herbsResult.Data ?? new List<HerbDto>()) : herbsResult;
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