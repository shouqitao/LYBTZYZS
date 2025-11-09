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
        /// 获取患者列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/patients")]
        Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取患者详情
        /// </summary>
        [Refit.Get("/api/v1/patients/{id}")]
        Task<ApiResponse<PatientDto>> GetPatientByIdAsync(Guid id);

        /// <summary>
        /// 创建患者
        /// </summary>
        [Refit.Post("/api/v1/patients")]
        Task<ApiResponse<PatientDto>> CreatePatientAsync([Refit.Body] PatientInputDto request);

        /// <summary>
        /// 更新患者
        /// </summary>
        [Refit.Put("/api/v1/patients/{id}")]
        Task<ApiResponse<PatientDto>> UpdatePatientAsync(Guid id, [Refit.Body] PatientInputDto request);

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        [Refit.Delete("/api/v1/patients/{id}")]
        Task<ApiResponse<ApiResponse>> DeletePatientAsync(Guid id);

        // ========== Epic #1934: 批量导入/导出功能 ==========

        /// <summary>
        /// 批量导入患者数据 (Epic #1934 FR-001)
        /// </summary>
        /// <param name="file">Excel文件流</param>
        /// <returns>导入结果（成功/失败/跳过数量及详细失败信息）</returns>
        [Refit.Multipart]
        [Refit.Post("/api/v1/patients/import")]
        Task<ApiResponse<BatchImportResultDto>> BatchImportAsync([Refit.AliasAs("file")] Refit.StreamPart file);

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
    }
}
