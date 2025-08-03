using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 药材服务实现
    /// </summary>
    public class HerbService : IHerbService
    {
        private readonly IApiService _apiService;
        private readonly IHerbApiService _herbApiService;

        public HerbService(IApiService apiService, IHerbApiService herbApiService)
        {
            _apiService = apiService;
            _herbApiService = herbApiService;
        }

        /// <summary>
        /// 获取药材列表
        /// </summary>
        public async Task<ApiResponse<List<HerbDto>>> GetHerbsAsync()
        {
            try
            {
                return await _herbApiService.GetHerbsAsync();
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<HerbDto>>
                {
                    IsSuccess = false,
                    Message = $"获取药材列表失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 分页查询药材
        /// </summary>
        public async Task<ApiResponse<PaginatedResult<HerbDto>>> GetPagedAsync(dynamic query)
        {
            try
            {
                return await _apiService.PostAsync<PaginatedResult<HerbDto>>("herbs/paged", query);
            }
            catch (Exception ex)
            {
                return new ApiResponse<PaginatedResult<HerbDto>>
                {
                    IsSuccess = false,
                    Message = $"分页查询药材失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        public async Task<ApiResponse<HerbDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _herbApiService.GetHerbByIdAsync(id);
                if (response.IsSuccess && response.Data != null)
                {
                    return response;
                }
                return new ApiResponse<HerbDto>
                {
                    IsSuccess = false,
                    Message = response.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<HerbDto>
                {
                    IsSuccess = false,
                    Message = $"获取药材详情失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        public async Task<ApiResponse<object>> AddAsync(HerbCreateDto dto)
        {
            try
            {
                // 直接使用传入的dto，它已经是HerbCreateDto类型
                var response = await _herbApiService.CreateHerbAsync(dto);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccess,
                    Message = response.Message,
                    Data = response.Data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"创建药材失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 编辑药材
        /// </summary>
        public Task<ApiResponse<object>> UpdateAsync(HerbUpdateDto dto)
        {
            try
            {
                // TODO: 需要传入ID参数，建议后端添加Id属性到UpdateHerbDto
                throw new NotImplementedException("UpdateAsync需要ID参数，但UpdateHerbDto中没有Id属性");
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"更新药材失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        public async Task<ApiResponse<object>> DeleteAsync(Guid id)
        {
            try
            {
                var response = await _herbApiService.DeleteHerbAsync(id);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccess,
                    Message = response.Message,
                    Data = response.Data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"删除药材失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取缺货药材列表
        /// </summary>
        public async Task<ApiResponse<List<HerbDto>>> GetOutOfStockAsync()
        {
            try
            {
                return await _herbApiService.GetLowStockHerbsAsync(0);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<HerbDto>>
                {
                    IsSuccess = false,
                    Message = $"获取缺货药材失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取即将过期的药材
        /// </summary>
        public async Task<ApiResponse<List<HerbDto>>> GetExpiringAsync(int days = 30)
        {
            try
            {
                return await _apiService.GetAsync<List<HerbDto>>($"herbs/expiring?days={days}");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<HerbDto>>
                {
                    IsSuccess = false,
                    Message = $"获取即将过期药材失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取可用药材列表
        /// </summary>
        public async Task<ApiResponse<List<HerbDto>>> GetAvailableAsync()
        {
            try
            {
                return await _apiService.GetAsync<List<HerbDto>>("herbs/available");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<HerbDto>>
                {
                    IsSuccess = false,
                    Message = $"获取可用药材失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        public async Task<ApiResponse<object>> ImportAsync(List<HerbCreateDto> herbs)
        {
            try
            {
                return await _apiService.PostAsync<object>("herbs/import", herbs);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"批量导入药材失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 更新药材状态
        /// </summary>
        public async Task<ApiResponse<object>> UpdateStatusAsync(BatchIdsDto dto)
        {
            try
            {
                // dto已经是BatchStatusUpdateDto类型，直接使用
                var response = await _herbApiService.BatchUpdateStatusAsync(dto);
                return new ApiResponse<object>
                {
                    IsSuccess = response.IsSuccess,
                    Message = response.Message,
                    Data = response.Data
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"更新药材状态失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取药材状态统计
        /// </summary>
        public async Task<ApiResponse<Dictionary<int, int>>> GetStatisticsAsync()
        {
            try
            {
                return await _apiService.GetAsync<Dictionary<int, int>>("herbs/statistics");
            }
            catch (Exception ex)
            {
                return new ApiResponse<Dictionary<int, int>>
                {
                    IsSuccess = false,
                    Message = $"获取药材统计失败: {ex.Message}"
                };
            }
        }
    }
}