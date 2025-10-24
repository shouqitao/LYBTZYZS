using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// Consultation API客户端接口（Refit定义）
    /// 包含CRUD操作和工作流方法
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

        /// <summary>
        /// 创建诊疗记录
        /// </summary>
        [Refit.Post("/api/v1/consultations")]
        Task<ApiResponse<ConsultationDto>> CreateConsultationAsync([Refit.Body] ConsultationCreateDto request);

        /// <summary>
        /// 更新诊疗记录
        /// </summary>
        [Refit.Put("/api/v1/consultations/{id}")]
        Task<ApiResponse<ConsultationDto>> UpdateConsultationAsync(Guid id, [Refit.Body] ConsultationUpdateDto request);

        /// <summary>
        /// 删除诊疗记录（软删除）
        /// </summary>
        [Refit.Delete("/api/v1/consultations/{id}")]
        Task<ApiResponse<ApiResponse>> DeleteConsultationAsync(Guid id);

        /// <summary>
        /// 完成辩证步骤（Step 1）
        /// Issue #1590: REQ-001 - 三步工作流优化-Step1
        /// </summary>
        [Refit.Post("/api/v1/consultations/{medicalCaseId}/complete-step1")]
        Task<ApiResponse<ConsultationStepDto>> CompleteStep1Async(
            Guid medicalCaseId,
            [Refit.Body] CompleteStep1Request request);
    }
}
