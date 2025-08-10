using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.WPF.Client.Core.Models;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Services.Interfaces;
using PagedResult = LYBT.WPF.Client.Core.Models.Common.PagedResult<LYBT.WPF.Client.Core.Models.Herbs.HerbInfo>;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 药材服务实现类
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

        /// <summary>
        /// 获取药材列表（实现IHerbService接口方法）
        /// </summary>
        public async Task<ApiResult<List<HerbDto>>> GetListAsync(HerbPagedQueryDto? query = null)
        {
            try
            {
                // 调用API获取数据
                var response = await _herbApiService.GetHerbsAsync(
                    page: 1,
                    pageSize: 1000,
                    keyword: query?.Keyword,
                    name: query?.Name,
                    origin: query?.Origin
                );
                
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return ApiResult<List<HerbDto>>.Success(response.Content.Items.ToList());
                }
                
                return ApiResult<List<HerbDto>>.Failure("获取药材列表失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材列表失败");
                return ApiResult<List<HerbDto>>.Failure($"获取药材列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分页查询药材
        /// </summary>
        public async Task<PagedResult> SearchHerbsAsync(HerbPagedQueryDto query)
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
                    var herbInfos = response.Content.Items.Select(ConvertToHerbInfo).ToList();
                    return new PagedResult
                    {
                        Items = herbInfos,
                        TotalCount = (int)response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                }
                return new PagedResult { Items = new List<HerbInfo>(), TotalCount = 0 };
            }
            catch (Refit.ApiException apiEx)
            {
                return new PagedResult
                {
                    Items = new List<HerbInfo>(),
                    TotalCount = 0,
                    ErrorMessage = apiEx.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "未授权访问，请先登录"
                        : $"API请求失败: {apiEx.StatusCode}"
                };
            }
            catch (System.Net.Http.HttpRequestException)
            {
                return new PagedResult
                {
                    Items = new List<HerbInfo>(),
                    TotalCount = 0,
                    ErrorMessage = "无法连接到服务器，请检查网络连接和API服务状态"
                };
            }
            catch (Exception ex)
            {
                return new PagedResult
                {
                    Items = new List<HerbInfo>(),
                    TotalCount = 0,
                    ErrorMessage = $"搜索药材失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取药材列表
        /// </summary>
        public async Task<List<HerbInfo>> GetHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.GetHerbsAsync(page: 1, pageSize: 1000);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content.Items.Select(ConvertToHerbInfo).ToList();
                }
                return new List<HerbInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材列表失败");
                throw new Exception($"获取药材列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        public async Task<HerbInfo?> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _herbApiService.GetHerbByIdAsync(id);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return ConvertDetailToHerbInfo(response.Content);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材详情失败");
                throw new Exception($"获取药材详情失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        public async Task<ServiceResult> CreateHerbAsync(HerbCreateDto dto)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _herbApiService.CreateHerbAsync(dto)
            );
            return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "操作失败");
        }

        /// <summary>
        /// 编辑药材
        /// </summary>
        public async Task<ServiceResult> UpdateHerbAsync(HerbUpdateDto dto)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _herbApiService.UpdateHerbAsync(dto.Id, dto)
            );
        }

        /// <summary>
        /// 删除药材（软删除）
        /// </summary>
        public async Task<ServiceResult> DeleteHerbAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _herbApiService.ToggleStatusAsync(id)
            );
        }

        /// <summary>
        /// 更新药材状态
        /// </summary>
        public async Task<ServiceResult> UpdateStatusAsync(Guid id, CommonStatusUpdateDto dto)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () =>
                await _herbApiService.UpdateStatusAsync(dto)
            );
        }

        /// <summary>
        /// 获取可用药材列表
        /// </summary>
        public async Task<List<HerbInfo>> GetAvailableHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.GetAvailableHerbsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content.Select(ConvertToHerbInfo).ToList();
                }
                return new List<HerbInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用药材失败");
                throw new Exception($"获取可用药材失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取缺货药材列表
        /// </summary>
        public async Task<List<HerbInfo>> GetOutOfStockHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.GetOutOfStockHerbsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content.Select(ConvertToHerbInfo).ToList();
                }
                return new List<HerbInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缺货药材失败");
                throw new Exception($"获取缺货药材失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取即将过期的药材
        /// </summary>
        public async Task<List<HerbInfo>> GetExpiringHerbsAsync(int days = 30)
        {
            try
            {
                var response = await _herbApiService.GetExpiringHerbsAsync(days);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content.Select(ConvertToHerbInfo).ToList();
                }
                return new List<HerbInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取即将过期药材失败");
                throw new Exception($"获取即将过期药材失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取药材状态统计
        /// </summary>
        public async Task<Dictionary<int, int>> GetStatisticsAsync()
        {
            try
            {
                var response = await _herbApiService.GetStatisticsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content;
                }
                return new Dictionary<int, int>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材统计失败");
                throw new Exception($"获取药材统计失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _herbApiService.ImportHerbsAsync(herbs)
            );
        }

        /// <summary>
        /// 导出药材数据
        /// </summary>
        public async Task<List<HerbInfo>> ExportHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.ExportHerbsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content.Select(ConvertDetailToHerbInfo).ToList();
                }
                return new List<HerbInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出药材数据失败");
                throw new Exception($"导出药材数据失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 按名称搜索药材
        /// </summary>
        public async Task<ServiceResult<List<HerbInfo>>> SearchByNameAsync(string name)
        {
            try
            {
                var response = await _herbApiService.GetHerbsAsync(
                    page: 1,
                    pageSize: 100,
                    keyword: name
                );

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var herbs = response.Content.Items?.Select(ConvertToHerbInfo).ToList() ?? new List<HerbInfo>();
                    return ServiceResult<List<HerbInfo>>.Success(herbs);
                }

                return ServiceResult<List<HerbInfo>>.Failure("搜索药材失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索药材失败");
                return ServiceResult<List<HerbInfo>>.Failure($"搜索药材时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 转换HerbDto到HerbInfo
        /// </summary>
        private HerbInfo ConvertToHerbInfo(HerbDto dto)
        {
            return new HerbInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                PinYinCode = dto.PinYinCode ?? "",
                Origin = dto.Origin,
                Spec = dto.Spec,
                Unit = dto.Unit,
                Price = dto.Price,
                Effect = dto.Effect,
                Usage = dto.Usage,
                Remark = dto.Remark
            };
        }

        /// <summary>
        /// 转换HerbDetailDto到HerbInfo
        /// </summary>
        private HerbInfo ConvertDetailToHerbInfo(HerbDetailDto dto)
        {
            return new HerbInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                PinYinCode = dto.PinYinCode ?? "",
                Origin = dto.Origin,
                Spec = dto.Spec,
                Unit = dto.Unit,
                Price = dto.Price,
                Effect = dto.Effect,
                Usage = dto.Usage,
                Remark = dto.Remark
            };
        }
    }
}