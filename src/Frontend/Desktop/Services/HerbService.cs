using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.Shared.Models.Common;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Core.Models.DTOs;
using LYBT.WPF.Client.Core.Interfaces.Services;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 药材服务实现
    /// </summary>
    public class HerbService : IHerbService
    {
        private readonly IApiService _apiService;

        public HerbService(IApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// 获取药材列表
        /// </summary>
        public async Task<ApiResponse<List<HerbInfo>>> GetHerbsAsync()
        {
            try
            {
                return await _apiService.GetAsync<List<HerbInfo>>("herbs");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<HerbInfo>>
                {
                    IsSuccess = false,
                    Message = $"获取药材列表失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 分页查询药材
        /// </summary>
        public async Task<ApiResponse<PagedResultDto<HerbInfo>>> GetPagedAsync(HerbPagedQueryDto query)
        {
            try
            {
                return await _apiService.PostAsync<PagedResultDto<HerbInfo>>("herbs/paged", query);
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResultDto<HerbInfo>>
                {
                    IsSuccess = false,
                    Message = $"分页查询药材失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        public async Task<ApiResponse<HerbDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                return await _apiService.GetAsync<HerbDetailDto>($"herbs/{id}");
            }
            catch (Exception ex)
            {
                return new ApiResponse<HerbDetailDto>
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
                return await _apiService.PostAsync<object>("herbs", dto);
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
        public async Task<ApiResponse<object>> UpdateAsync(HerbEditDto dto)
        {
            try
            {
                return await _apiService.PutAsync<object>("herbs", dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    Message = $"更新药材失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        public async Task<ApiResponse<object>> DeleteAsync(Guid id)
        {
            try
            {
                return await _apiService.DeleteAsync<object>($"herbs/{id}");
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
        public async Task<ApiResponse<List<HerbInfo>>> GetOutOfStockAsync()
        {
            try
            {
                return await _apiService.GetAsync<List<HerbInfo>>("herbs/out-of-stock");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<HerbInfo>>
                {
                    IsSuccess = false,
                    Message = $"获取缺货药材失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取即将过期的药材
        /// </summary>
        public async Task<ApiResponse<List<HerbInfo>>> GetExpiringAsync(int days = 30)
        {
            try
            {
                return await _apiService.GetAsync<List<HerbInfo>>($"herbs/expiring?days={days}");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<HerbInfo>>
                {
                    IsSuccess = false,
                    Message = $"获取即将过期药材失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取可用药材列表
        /// </summary>
        public async Task<ApiResponse<List<HerbInfo>>> GetAvailableAsync()
        {
            try
            {
                return await _apiService.GetAsync<List<HerbInfo>>("herbs/available");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<HerbInfo>>
                {
                    IsSuccess = false,
                    Message = $"获取可用药材失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        public async Task<ApiResponse<object>> ImportAsync(List<HerbImportDto> herbs)
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
        public async Task<ApiResponse<object>> UpdateStatusAsync(HerbStatusUpdateDto dto)
        {
            try
            {
                return await _apiService.PatchAsync<object>("herbs/status", dto);
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