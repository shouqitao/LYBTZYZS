using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// Consultation API客户端接口（Refit定义）- Read-Only（Issue #1606）
    /// 所有Write操作已迁移至MedicalCaseController聚合根
    /// </summary>
    public interface IConsultationApi
    {
        /// <summary>
        /// 获取诊疗记录列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/consultations")]
        Task<ApiResponse<PagedResult<ConsultationDto>>> GetConsultationsAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 根据医案ID获取诊疗记录列表
        /// </summary>
        [Refit.Get("/api/v1/consultations/by-medicalcase/{medicalCaseId}")]
        Task<ApiResponse<List<ConsultationDto>>> GetConsultationsByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 获取诊疗记录详情
        /// </summary>
        [Refit.Get("/api/v1/consultations/{id}")]
        Task<ApiResponse<ConsultationDto>> GetConsultationByIdAsync(Guid id);

        // ========== Write方法已删除（Issue #1606 Phase 1）==========
        // CreateConsultationAsync 已删除，请使用 POST /api/v1/medicalcases/with-details
        // UpdateConsultationAsync 已删除，请使用 PUT /api/v1/medicalcases/{id}/consultation
        // DeleteConsultationAsync 已删除，请使用 DELETE /api/v1/medicalcases/{id}（级联删除）
        // CompleteStep1Async 已删除，请使用 POST /api/v1/medicalcases/{id}/complete-step1
    }
}
