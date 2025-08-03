using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.WPF.Client.Core.Models.Herbs;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 药材服务接口（对应后端IHerbService）
    /// </summary>
    public interface IHerbService
    {
        /// <summary>
        /// 获取药材列表
        /// </summary>
        Task<ApiResponse<List<HerbDto>>> GetHerbsAsync();

        /// <summary>
        /// 分页查询药材
        /// </summary>
        Task<ApiResponse<PaginatedResult<HerbDto>>> GetPagedAsync(dynamic query);

        /// <summary>
        /// 获取药材详情
        /// </summary>
        Task<ApiResponse<HerbDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 新增药材
        /// </summary>
        Task<ApiResponse<object>> AddAsync(HerbCreateDto dto);

        /// <summary>
        /// 编辑药材
        /// </summary>
        Task<ApiResponse<object>> UpdateAsync(HerbUpdateDto dto);

        /// <summary>
        /// 删除药材
        /// </summary>
        Task<ApiResponse<object>> DeleteAsync(Guid id);

        /// <summary>
        /// 获取缺货药材列表
        /// </summary>
        Task<ApiResponse<List<HerbDto>>> GetOutOfStockAsync();

        /// <summary>
        /// 获取即将过期的药材
        /// </summary>
        Task<ApiResponse<List<HerbDto>>> GetExpiringAsync(int days = 30);

        /// <summary>
        /// 获取可用药材列表
        /// </summary>
        Task<ApiResponse<List<HerbDto>>> GetAvailableAsync();

        /// <summary>
        /// 批量导入药材
        /// </summary>
        Task<ApiResponse<object>> ImportAsync(List<HerbCreateDto> herbs);

        /// <summary>
        /// 更新药材状态
        /// </summary>
        Task<ApiResponse<object>> UpdateStatusAsync(BatchIdsDto dto);

        /// <summary>
        /// 获取药材状态统计
        /// </summary>
        Task<ApiResponse<Dictionary<int, int>>> GetStatisticsAsync();
    }
}