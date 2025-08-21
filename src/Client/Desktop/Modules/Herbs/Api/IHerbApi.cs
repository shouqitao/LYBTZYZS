using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Modules.Herbs.Api
{
    /// <summary>
    /// 药材API客户端接口 - UltraThink v2.0简化版
    /// 专注处方用药管理，删除库存管理功能
    /// </summary>
    public interface IHerbApi
    {
        /// <summary>
        /// 获取药材列表（支持分页和查询）
        /// </summary>
        [Get("/api/v1/herbs")]
        Task<Refit.ApiResponse<PagedData<HerbDto>>> GetHerbsAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null,
            [Query] string? name = null,
            [Query] string? origin = null,
            [Query] string? effect = null,
            [Query] string? usage = null,
            [Query] int? status = null,
            [Query] decimal? minPrice = null,
            [Query] decimal? maxPrice = null);

        /// <summary>
        /// 获取分页药材列表（兼容性别名）
        /// </summary>
        [Get("/api/v1/herbs")]
        Task<Refit.ApiResponse<PagedData<HerbDto>>> GetPagedAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null);

        /// <summary>
        /// 获取药材详情
        /// </summary>
        [Get("/api/v1/herbs/{id}")]
        Task<Refit.ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id);

        /// <summary>
        /// 创建药材
        /// </summary>
        [Post("/api/v1/herbs")]
        Task<Refit.ApiResponse<HerbDto>> CreateHerbAsync([Body] HerbCreateDto dto);

        /// <summary>
        /// 更新药材
        /// </summary>
        [Put("/api/v1/herbs/{id}")]
        Task<Refit.ApiResponse<HerbDto>> UpdateHerbAsync(Guid id, [Body] HerbUpdateDto dto);

        /// <summary>
        /// 更新药材状态
        /// </summary>
        [Patch("/api/v1/herbs/status")]
        Task<Refit.ApiResponse<object>> UpdateStatusAsync([Body] CommonStatusUpdateDto dto);

        /// <summary>
        /// 切换药材状态
        /// </summary>
        [Patch("/api/v1/herbs/{id}/toggle-status")]
        Task<Refit.ApiResponse<object>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 获取可用药材列表（用于处方开具）
        /// </summary>
        [Get("/api/v1/herbs/available")]
        Task<Refit.ApiResponse<List<HerbDto>>> GetAvailableHerbsAsync();

        /// <summary>
        /// 获取药材状态统计
        /// </summary>
        [Get("/api/v1/herbs/statistics")]
        Task<Refit.ApiResponse<Dictionary<int, int>>> GetStatisticsAsync();

        /// <summary>
        /// 批量导入药材
        /// </summary>
        [Post("/api/v1/herbs/import")]
        Task<Refit.ApiResponse<int>> ImportHerbsAsync([Body] List<HerbImportDto> herbs);

        /// <summary>
        /// 导出药材数据
        /// </summary>
        [Get("/api/v1/herbs/export")]
        Task<Refit.ApiResponse<List<HerbDetailDto>>> ExportHerbsAsync();
        
        /// <summary>
        /// 获取药材导入模板
        /// </summary>
        [Get("/api/v1/herbs/import-template")]
        Task<Refit.ApiResponse<byte[]>> GetImportTemplateAsync();
    }
}