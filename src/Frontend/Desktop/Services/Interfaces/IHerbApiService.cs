using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Herbs;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 药材API服务接口
    /// </summary>
    public interface IHerbApiService
    {
        /// <summary>
        /// 获取药材列表
        /// </summary>
        [Get("/api/v1/herbs")]
        Task<ApiResponse<List<HerbDto>>> GetHerbsAsync([Query] string? search = null, [Query] int? status = null);

        /// <summary>
        /// 获取药材详情
        /// </summary>
        [Get("/api/v1/herbs/{id}")]
        Task<ApiResponse<HerbDto>> GetHerbByIdAsync(Guid id);

        /// <summary>
        /// 创建药材
        /// </summary>
        [Post("/api/v1/herbs")]
        Task<ApiResponse<HerbDto>> CreateHerbAsync([Body] CreateHerbDto dto);

        /// <summary>
        /// 更新药材
        /// </summary>
        [Put("/api/v1/herbs/{id}")]
        Task<ApiResponse<HerbDto>> UpdateHerbAsync(Guid id, [Body] UpdateHerbDto dto);

        /// <summary>
        /// 删除药材
        /// </summary>
        [Delete("/api/v1/herbs/{id}")]
        Task<ApiResponse<bool>> DeleteHerbAsync(Guid id);

        /// <summary>
        /// 批量更新药材状态
        /// </summary>
        [Patch("/api/v1/herbs/batch-status")]
        Task<ApiResponse<int>> BatchUpdateStatusAsync([Body] BatchStatusUpdateDto dto);

        /// <summary>
        /// 更新库存
        /// </summary>
        [Post("/api/v1/herbs/{id}/update-stock")]
        Task<ApiResponse<bool>> UpdateStockAsync(Guid id, [Body] UpdateStockDto dto);

        /// <summary>
        /// 获取低库存药材
        /// </summary>
        [Get("/api/v1/herbs/low-stock")]
        Task<ApiResponse<List<HerbDto>>> GetLowStockHerbsAsync([Query] decimal threshold = 50);
    }
}