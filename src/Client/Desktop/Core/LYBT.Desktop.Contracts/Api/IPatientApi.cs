using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
namespace LYBT.Desktop.Contracts.Api
{

    /// <summary>
    /// 患者API客户端接口 - UltraThink统一标准
    /// 移动到shared层以确保前后端契约一致性
    /// </summary>
    public interface IPatientApi
    {
        /// <summary>
        /// 获取患者列表（分页查询）
        /// </summary>
        [Refit.Get("/api/v1/patients")]
        Task<ApiResponse<PagedResult<PatientListDto>>> GetPatientsAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取患者详情
        /// </summary>
        [Refit.Get("/api/v1/patients/{id}")]
        Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(Guid id);

        /// <summary>
        /// 创建患者
        /// </summary>
        [Refit.Post("/api/v1/patients")]
        Task<ApiResponse<PatientDetailDto>> CreatePatientAsync([Refit.Body] PatientInputDto request);

        /// <summary>
        /// 更新患者
        /// </summary>
        [Refit.Put("/api/v1/patients/{id}")]
        Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(Guid id, [Refit.Body] PatientInputDto request);

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        [Refit.Delete("/api/v1/patients/{id}")]
        Task<ApiResponse> DeletePatientAsync(Guid id);

        // ========== Epic #1934: 批量导入/导出功能 ==========

        /// <summary>
        /// 批量导入患者数据 (Issue #2004 Task 2.11)
        /// Desktop主导模式：Desktop解析Excel并组装DTO，API接收并批量创建
        /// Note: Server端需要实现对应的 POST /api/v1/patients/batch-import endpoint
        /// </summary>
        [Refit.Post("/api/v1/patients/batch-import")]
        Task<ApiResponse<PatientBatchImportResultDto>> BatchImportAsync([Refit.Body] PatientBatchImportInputDto request);

        /// <summary>
        /// 下载患者导入模板 (Epic #1934 FR-002)
        /// </summary>
        /// <returns>Excel模板文件流（包含示例数据）</returns>
        [Refit.Get("/api/v1/patients/import-template")]
        Task<HttpResponseMessage> ExportTemplateAsync();

        /// <summary>
        /// 导出患者数据到Excel (Epic #1934 FR-003)
        /// </summary>
        /// <param name="keyword">搜索关键词（可选）</param>
        /// <returns>包含患者数据的Excel文件流</returns>
        [Refit.Get("/api/v1/patients/export")]
        Task<HttpResponseMessage> ExportPatientsAsync([Refit.Query] string? keyword = null);

        // ========== OpenSpec: optimize-module-list-ui - 恢复功能 ==========

        /// <summary>
        /// 恢复已删除的患者
        /// 注：患者实体无Status字段，因此无ToggleStatus方法
        /// </summary>
        [Refit.Post("/api/v1/patients/{id}/restore")]
        Task<ApiResponse<PatientDetailDto>> RestoreAsync(Guid id);

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量删除患者
        /// </summary>
        [Refit.Post("/api/v1/patients/batch-delete")]
        Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);
    }
}
