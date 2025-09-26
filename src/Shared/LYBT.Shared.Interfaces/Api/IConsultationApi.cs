using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Shared.Interfaces.Api
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
        Task<Refit.ApiResponse<PagedResult<ConsultationDto>>> GetConsultationsAsync(
            [Refit.Query] int pageIndex = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? searchTerm = null);

        /// <summary>
        /// 获取诊疗记录详情
        /// </summary>
        [Refit.Get("/api/v1/consultations/{id}")]
        Task<Refit.ApiResponse<ConsultationDto>> GetConsultationByIdAsync(Guid id);

        /// <summary>
        /// 创建诊疗记录
        /// </summary>
        [Refit.Post("/api/v1/consultations")]
        Task<Refit.ApiResponse<ConsultationDto>> CreateConsultationAsync([Refit.Body] ConsultationCreateDto request);

        /// <summary>
        /// 更新诊疗记录
        /// </summary>
        [Refit.Put("/api/v1/consultations/{id}")]
        Task<Refit.ApiResponse<ConsultationDto>> UpdateConsultationAsync(Guid id, [Refit.Body] ConsultationUpdateDto request);

        /// <summary>
        /// 删除诊疗记录
        /// </summary>
        [Refit.Delete("/api/v1/consultations/{id}")]
        Task<Refit.ApiResponse<object>> DeleteConsultationAsync(Guid id);
    }
}