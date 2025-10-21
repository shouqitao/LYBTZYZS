using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 诊疗API客户端接口 - 简化版，只包含基础CRUD
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
        /// 删除诊疗记录
        /// </summary>
        [Refit.Delete("/api/v1/consultations/{id}")]
        Task<ApiResponse<ApiResponse>> DeleteConsultationAsync(Guid id);

        /// <summary>
        /// 根据医案ID获取诊疗记录列表
        /// </summary>
        [Refit.Get("/api/v1/consultations/medicalcase/{medicalCaseId}")]
        Task<ApiResponse<List<ConsultationDto>>> GetConsultationsByMedicalCaseIdAsync(Guid medicalCaseId);
    }
}
