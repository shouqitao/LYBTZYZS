using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
namespace LYBT.Shared.Interfaces.Api
{

    /// <summary>
    /// 药材API客户端接口 - UltraThink v2.0简化版
    /// 专注处方用药管理，删除库存管理功能
    /// 移动到shared层以确保前后端契约一致性
    /// </summary>
    public interface IHerbApi
    {

        /// <summary>
        /// 获取药材列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/herbs")]
        Task<Refit.ApiResponse<PagedResult<HerbDto>>> GetHerbsAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null,
            [Refit.Query] string? name = null,
            [Refit.Query] string? origin = null,
            [Refit.Query] string? effect = null,
            [Refit.Query] string? usage = null,
            [Refit.Query] int? status = null,
            [Refit.Query] decimal? minPrice = null,
            [Refit.Query] decimal? maxPrice = null);

        /// <summary>
        /// 获取分页药材列表（兼容性别名）
        /// </summary>
        [Refit.Get("/api/v1/herbs")]
        Task<Refit.ApiResponse<PagedResult<HerbDto>>> GetPagedAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取药材详情
        /// </summary>
        [Refit.Get("/api/v1/herbs/{id}")]
        Task<Refit.ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id);

        /// <summary>
        /// 创建药材
        /// </summary>
        [Refit.Post("/api/v1/herbs")]
        Task<Refit.ApiResponse<HerbDto>> CreateHerbAsync([Refit.Body] HerbCreateDto dto);

        /// <summary>
        /// 更新药材
        /// </summary>
        [Refit.Put("/api/v1/herbs/{id}")]
        Task<Refit.ApiResponse<HerbDto>> UpdateHerbAsync(Guid id, [Refit.Body] HerbUpdateDto dto);

        /// <summary>
        /// 更新药材状态
        /// </summary>
        [Refit.Patch("/api/v1/herbs/status")]
        Task<Refit.ApiResponse<object>> UpdateStatusAsync([Refit.Body] CommonStatusUpdateDto dto);

        /// <summary>
        /// 切换药材状态
        /// </summary>
        [Refit.Patch("/api/v1/herbs/{id}/toggle-status")]
        Task<Refit.ApiResponse<object>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 获取可用药材列表（用于处方开具）
        /// </summary>
        [Refit.Get("/api/v1/herbs/available")]
        Task<Refit.ApiResponse<List<HerbDto>>> GetAvailableHerbsAsync();

        /// <summary>
        /// 获取药材状态统计
        /// </summary>
        [Refit.Get("/api/v1/herbs/statistics")]
        Task<Refit.ApiResponse<Dictionary<int, int>>> GetStatisticsAsync();

        /// <summary>
        /// 批量导入药材
        /// </summary>
        [Refit.Post("/api/v1/herbs/import")]
        Task<Refit.ApiResponse<int>> ImportHerbsAsync([Refit.Body] List<HerbImportDto> herbs);

        /// <summary>
        /// 导出药材数据
        /// </summary>
        [Refit.Get("/api/v1/herbs/export")]
        Task<Refit.ApiResponse<List<HerbDetailDto>>> ExportHerbsAsync();

        /// <summary>
        /// 获取药材导入模板
        /// </summary>
        [Refit.Get("/api/v1/herbs/import-template")]
        Task<Refit.ApiResponse<byte[]>> GetImportTemplateAsync();
    }
}
