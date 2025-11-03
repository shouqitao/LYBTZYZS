using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 处方API客户端接口 - Read-Only（Issue #1606）
    /// 所有Write操作已迁移至MedicalCaseController聚合根
    /// </summary>
    public interface IPrescriptionApi
    {
        /// <summary>
        /// 获取处方列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/prescriptions")]
        Task<ApiResponse<PagedResult<PrescriptionDto>>> GetPrescriptionsAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取处方详情
        /// </summary>
        [Refit.Get("/api/v1/prescriptions/{id}")]
        Task<ApiResponse<PrescriptionDto>> GetPrescriptionByIdAsync(Guid id);

        /// <summary>
        /// 根据医案ID获取处方列表
        /// </summary>
        [Refit.Get("/api/v1/prescriptions/medicalcase/{medicalCaseId}")]
        Task<ApiResponse<List<PrescriptionDto>>> GetPrescriptionsByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 获取患者最近处方列表 (Issue #1371 ENTRY-13)
        /// </summary>
        [Refit.Get("/api/v1/prescriptions/patient/{patientId}/recent")]
        Task<ApiResponse<List<PrescriptionSearchResultDto>>> GetPatientRecentPrescriptionsAsync(
            Guid patientId,
            [Refit.Query] int count = 5);

        // ========== Write方法已删除（Issue #1606 Phase 1）==========
        // CreatePrescriptionAsync 已删除，请使用 POST /api/v1/medicalcases/with-details
        // UpdatePrescriptionAsync 已删除，请使用 PUT /api/v1/medicalcases/{id}/prescription
        // DeletePrescriptionAsync 已删除，请使用 DELETE /api/v1/medicalcases/{id}（级联删除）
        // SoftDeletePrescriptionAsync 已删除，请使用 DELETE /api/v1/medicalcases/{id}/soft
        // ImportFormulaIntoPrescriptionAsync 已删除，请使用 POST /api/v1/medicalcases/{id}/prescription/import-formula/{formulaId}
    }
}
