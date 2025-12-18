using System.Net.Http;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 草药API客户端接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IHerbApi
    {
        /// <summary>
        /// 获取草药列表（支持分页和查询）- 有缓存
        /// </summary>
        [Refit.Get("/api/v1/herbs")]
        Task<ApiResponse<PagedResult<HerbDetailDto>>> GetHerbsAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取草药列表（返回HerbListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        [Refit.Get("/api/v1/herbs/list")]
        Task<ApiResponse<PagedResult<HerbListDto>>> GetHerbsListAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null,
            [Refit.Query] string? category = null);

        /// <summary>
        /// 获取草药详情
        /// </summary>
        [Refit.Get("/api/v1/herbs/{id}")]
        Task<ApiResponse<HerbDetailDto>> GetHerbByIdAsync(Guid id);

        /// <summary>
        /// 创建草药
        /// </summary>
        [Refit.Post("/api/v1/herbs")]
        Task<ApiResponse<HerbDetailDto>> CreateHerbAsync([Refit.Body] HerbInputDto request);

        /// <summary>
        /// 更新草药
        /// </summary>
        [Refit.Put("/api/v1/herbs/{id}")]
        Task<ApiResponse<HerbDetailDto>> UpdateHerbAsync(Guid id, [Refit.Body] HerbInputDto request);

        /// <summary>
        /// 删除草药
        /// </summary>
        [Refit.Delete("/api/v1/herbs/{id}")]
        Task<ApiResponse<ApiResponse>> DeleteHerbAsync(Guid id);

        // ========== Epic #1962: 批量导入/导出功能（参考患者模块） ==========

        /// <summary>
        /// 批量导入药材数据
        /// </summary>
        [Refit.Multipart]
        [Refit.Post("/api/v1/herbs/import")]
        Task<ApiResponse<HerbBatchImportResultDto>> BatchImportAsync(
            [Refit.AliasAs("file")] Refit.StreamPart file);

        /// <summary>
        /// 下载药材导入模板
        /// </summary>
        [Refit.Get("/api/v1/herbs/import-template")]
        Task<HttpResponseMessage> ExportTemplateAsync();

        /// <summary>
        /// 导出药材数据到Excel
        /// </summary>
        [Refit.Get("/api/v1/herbs/export")]
        Task<HttpResponseMessage> ExportHerbsAsync([Refit.Query] string? keyword = null);

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复 ==========

        /// <summary>
        /// 切换药材状态（启用/禁用）
        /// </summary>
        [Refit.Post("/api/v1/herbs/{id}/toggle-status")]
        Task<ApiResponse<HerbDetailDto>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 恢复已删除的药材
        /// </summary>
        [Refit.Post("/api/v1/herbs/{id}/restore")]
        Task<ApiResponse<HerbDetailDto>> RestoreAsync(Guid id);

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量删除药材（软删除）
        /// </summary>
        [Refit.Post("/api/v1/herbs/batch-delete")]
        Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);

        /// <summary>
        /// 批量启用药材
        /// </summary>
        [Refit.Post("/api/v1/herbs/batch-enable")]
        Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync([Refit.Body] BatchDeleteInputDto request);

        /// <summary>
        /// 批量禁用药材
        /// </summary>
        [Refit.Post("/api/v1/herbs/batch-disable")]
        Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync([Refit.Body] BatchDeleteInputDto request);
    }
}
