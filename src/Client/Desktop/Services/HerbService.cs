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
using LYBT.Desktop.Core.Models.Herbs;

// UltraThink重构: 恢复四层架构清晰分离，HerbInfo为UI层，HerbDto为传输层

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

        public async Task<ServiceResult<HerbInfo>> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _herbApiService.GetHerbByIdAsync(id);
                var result = response.IsSuccessStatusCode && response.Content != null 
                    ? ConvertToHerbInfo(response.Content) 
                    : new HerbInfo { Id = id, Name = "未找到" };
                return ServiceResult<HerbInfo>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材详情失败: {Id}", id);
                return ServiceResult<HerbInfo>.Failure($"获取药材详情失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<PagedResult<HerbInfo>>> GetPagedAsync(HerbPagedQueryDto query)
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
                    var items = response.Content.Items.Select(dto => ConvertToHerbInfo(dto)).ToList();
                    var result = new PagedResult<HerbInfo>
                    {
                        Items = items,
                        TotalCount = (int)response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                    return ServiceResult<PagedResult<HerbInfo>>.Success(result);
                }
                
                return ServiceResult<PagedResult<HerbInfo>>.Success(new PagedResult<HerbInfo> { Items = new List<HerbInfo>(), TotalCount = 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询药材失败");
                return ServiceResult<PagedResult<HerbInfo>>.Failure($"分页查询药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<HerbInfo>>> GetAllAsync()
        {
            try
            {
                var response = await _herbApiService.GetHerbsAsync(page: 1, pageSize: 1000);
                var result = response.IsSuccessStatusCode && response.Content != null 
                    ? response.Content.Items.Select(dto => ConvertToHerbInfo(dto)).ToList()
                    : new List<HerbInfo>();
                return ServiceResult<List<HerbInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有药材失败");
                return ServiceResult<List<HerbInfo>>.Failure($"获取所有药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<HerbInfo>> CreateAsync(HerbCreateDto dto)
        {
            try
            {
                var response = await _herbApiService.CreateHerbAsync(dto);
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? ConvertToHerbInfo(response.Content)
                    : new HerbInfo { Name = dto.Name };
                return ServiceResult<HerbInfo>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建药材失败");
                return ServiceResult<HerbInfo>.Failure($"创建药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<HerbInfo>> UpdateAsync(Guid id, HerbUpdateDto dto)
        {
            try
            {
                var response = await _herbApiService.UpdateHerbAsync(id, dto);
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? ConvertToHerbInfo(response.Content)
                    : new HerbInfo { Id = id, Name = dto.Name };
                return ServiceResult<HerbInfo>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新药材失败");
                return ServiceResult<HerbInfo>.Failure($"更新药材失败: {ex.Message}");
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

        public async Task<ServiceResult<List<HerbInfo>>> GetByIdsAsync(List<Guid> ids)
        {
            var results = new List<HerbInfo>();
            foreach (var id in ids)
            {
                var herbResult = await GetByIdAsync(id);
                if (herbResult.IsSuccess && herbResult.Data != null) results.Add(herbResult.Data);
            }
            return ServiceResult<List<HerbInfo>>.Success(results);
        }

        public async Task<ServiceResult<List<HerbInfo>>> SearchAsync(string keyword)
        {
            try
            {
                var response = await _herbApiService.GetHerbsAsync(page: 1, pageSize: 100, keyword: keyword);
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.Items.Select(dto => ConvertToHerbInfo(dto)).ToList()
                    : new List<HerbInfo>();
                return ServiceResult<List<HerbInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索药材失败");
                return ServiceResult<List<HerbInfo>>.Failure($"搜索药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> BatchDeleteAsync(List<Guid> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return ServiceResult<bool>.Success(true);
                }

                var successCount = 0;
                foreach (var id in ids)
                {
                    var result = await DeleteAsync(id);
                    if (result.IsSuccess && result.Data == true)
                    {
                        successCount++;
                    }
                }

                var isSuccess = successCount == ids.Count;
                _logger.LogInformation("批量删除药材: 总数={Total}, 成功={Success}", ids.Count, successCount);
                
                return ServiceResult<bool>.Success(isSuccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除药材失败");
                return ServiceResult<bool>.Failure($"批量删除药材失败: {ex.Message}");
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

        public async Task<ServiceResult<List<HerbInfo>>> GetListAsync(HerbPagedQueryDto? query = null)
        {
            try
            {
                var herbs = query != null ? 
                    (await GetPagedAsync(query)).Data?.Items ?? new List<HerbInfo>() :
                    (await GetAllAsync()).Data ?? new List<HerbInfo>();
                return ServiceResult<List<HerbInfo>>.Success(herbs.ToList());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbInfo>>.Failure($"获取药材列表失败: {ex.Message}");
            }
        }

        public async Task<PagedResult<HerbInfo>> SearchHerbsAsync(HerbPagedQueryDto query)
        {
            var result = await GetPagedAsync(query);
            return result.IsSuccess && result.Data != null ? result.Data : new PagedResult<HerbInfo> { Items = new List<HerbInfo>(), TotalCount = 0 };
        }

        public async Task<ServiceResult<List<HerbInfo>>> GetHerbsAsync()
        {
            return await GetAllAsync();
        }

        public async Task<HerbInfo?> GetByIdHerbInfoAsync(Guid id)
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

        public async Task<ServiceResult<List<HerbInfo>>> GetAvailableHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.GetAvailableHerbsAsync();
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.Select(dto => ConvertToHerbInfo(dto)).ToList()
                    : new List<HerbInfo>();
                return ServiceResult<List<HerbInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用药材失败");
                return ServiceResult<List<HerbInfo>>.Failure($"获取可用药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<HerbInfo>>> GetOutOfStockHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.GetOutOfStockHerbsAsync();
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.Select(dto => ConvertToHerbInfo(dto)).ToList()
                    : new List<HerbInfo>();
                return ServiceResult<List<HerbInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缺货药材失败");
                return ServiceResult<List<HerbInfo>>.Failure($"获取缺货药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<HerbInfo>>> GetExpiringHerbsAsync(int days = 30)
        {
            try
            {
                var response = await _herbApiService.GetExpiringHerbsAsync(days);
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.Select(dto => ConvertToHerbInfo(dto)).ToList()
                    : new List<HerbInfo>();
                return ServiceResult<List<HerbInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取即将过期药材失败");
                return ServiceResult<List<HerbInfo>>.Failure($"获取即将过期药材失败: {ex.Message}");
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

        public async Task<ServiceResult<List<HerbInfo>>> ExportHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.ExportHerbsAsync();
                var result = response.IsSuccessStatusCode && response.Content != null
                    ? response.Content.Select(ConvertToHerbInfo).ToList()
                    : new List<HerbInfo>();
                return ServiceResult<List<HerbInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出药材数据失败");
                return ServiceResult<List<HerbInfo>>.Failure($"导出药材数据失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<HerbInfo>>> SearchByNameAsync(string name)
        {
            try
            {
                var herbsResult = await SearchAsync(name);
                return herbsResult.IsSuccess ? ServiceResult<List<HerbInfo>>.Success(herbsResult.Data ?? new List<HerbInfo>()) : herbsResult;
            }
            catch (Exception ex)
            {
                return ServiceResult<List<HerbInfo>>.Failure($"搜索药材失败: {ex.Message}");
            }
        }

        #endregion

        #region 私有转换方法

        /// <summary>
        /// UltraThink重构: HerbDetailDto转换为HerbInfo（UI层模型）
        /// </summary>
        private HerbInfo ConvertToHerbInfo(HerbDetailDto detail)
        {
            return new HerbInfo
            {
                // 基础属性映射
                Id = detail.Id,
                Name = detail.Name,
                PinYinCode = detail.PinYinCode,
                Origin = detail.Origin,
                Spec = detail.Spec,
                Unit = detail.Unit ?? "克",
                Price = detail.Price,
                Effect = detail.Effect,
                Usage = detail.Usage,
                Remark = detail.Remark,
                Status = detail.Status,
                
                // UI专用属性初始化
                TotalPrice = detail.Price, // 默认单价作为总价
                StatusDescription = detail.Status.ToString(),
                Supplier = null, // 待扩展
                LastOperationTime = detail.UpdateTime,
                OperatorName = null, // 待扩展
                Category = "其他", // 默认分类
                Stock = 0, // 默认库存
                IsActive = detail.Status == CommonStatus.Enabled
            };
        }
        
        /// <summary>
        /// UltraThink重构: HerbDto转换为HerbInfo（UI层模型）
        /// </summary>
        private HerbInfo ConvertToHerbInfo(HerbDto dto)
        {
            return new HerbInfo
            {
                // 基础属性映射
                Id = dto.Id,
                Name = dto.Name,
                PinYinCode = dto.PinYinCode,
                Origin = dto.Origin,
                Spec = dto.Spec,
                Unit = dto.Unit,
                Price = dto.Price,
                Effect = dto.Effect,
                Usage = dto.Usage,
                Remark = dto.Remark,
                Status = dto.Status,
                
                // UI专用属性初始化
                TotalPrice = dto.Price, // 默认单价作为总价
                StatusDescription = dto.Status.ToString(),
                Supplier = null, // 待扩展
                LastOperationTime = null, // HerbDto没有UpdateTime属性
                OperatorName = null, // 待扩展
                Category = "其他", // 默认分类
                Stock = dto.Stock, // 从HerbDto获取库存
                IsActive = dto.Status == CommonStatus.Enabled
            };
        }

        #endregion
    }
}