using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
namespace LYBT.Shared.Interfaces.Api
{

    /// <summary>
    /// 诊疗API客户端接口 - UltraThink统一标准
    /// </summary>
    public interface IConsultationApi
    {

        /// <summary>
        /// 分页查询诊疗记录
        /// </summary>
        [Refit.Get("/api/v1/consultation")]
        Task<Refit.ApiResponse<PagedResult<ConsultationDto>>> GetConsultationsAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 10,
            [Refit.Query] string? keyword = null,
            [Refit.Query] Guid? doctorId = null,
            [Refit.Query] Guid? patientId = null,
            [Refit.Query] DateTime? startDate = null,
            [Refit.Query] DateTime? endDate = null,
            [Refit.Query] int? status = null);

        /// <summary>
        /// 获取诊疗详情
        /// </summary>
        [Refit.Get("/api/v1/consultation/{id}")]
        Task<Refit.ApiResponse<ConsultationDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 根据医疗案例ID获取诊疗信息
        /// </summary>
        [Refit.Get("/api/v1/consultation/medical-case/{medicalCaseId}")]
        Task<Refit.ApiResponse<ConsultationDetailDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 开始诊疗
        /// </summary>
        [Refit.Post("/api/v1/consultation/start")]
        Task<Refit.ApiResponse<ConsultationDetailDto>> StartConsultationAsync([Refit.Body] ConsultationStartDto dto);

        /// <summary>
        /// 更新诊疗信息
        /// </summary>
        [Refit.Put("/api/v1/consultation/{id}")]
        Task<Refit.ApiResponse<ConsultationDetailDto>> UpdateConsultationAsync(Guid id, [Refit.Body] ConsultationUpdateDto dto);

        /// <summary>
        /// 完成诊疗
        /// </summary>
        [Refit.Post("/api/v1/consultation/{id}/complete")]
        Task<Refit.ApiResponse<object>> CompleteConsultationAsync(Guid id, [Refit.Body] ConsultationCompleteDto dto);

        /// <summary>
        /// 取消诊疗
        /// </summary>
        [Refit.Post("/api/v1/consultation/{id}/cancel")]
        Task<Refit.ApiResponse<object>> CancelConsultationAsync(Guid id, [Refit.Body] string reason);

        /// <summary>
        /// 获取统计信息
        /// </summary>
        [Refit.Get("/api/v1/consultation/statistics")]
        Task<Refit.ApiResponse<object>> GetStatisticsAsync(
            [Refit.Query] DateTime? startDate = null,
            [Refit.Query] DateTime? endDate = null);

        /// <summary>
        /// 获取医生今日诊疗列表
        /// </summary>
        [Refit.Get("/api/v1/consultation/doctor/{doctorId}/today")]
        Task<Refit.ApiResponse<List<ConsultationDto>>> GetTodayConsultationsByDoctorAsync(Guid doctorId);

        /// <summary>
        /// 获取患者历史诊疗记录
        /// </summary>
        [Refit.Get("/api/v1/consultation/patient/{patientId}/history")]
        Task<Refit.ApiResponse<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId);

        /// <summary>
        /// 统计医生诊疗数量
        /// </summary>
        [Refit.Get("/api/v1/consultation/doctor/{doctorId}/count")]
        Task<Refit.ApiResponse<int>> GetDoctorConsultationCountAsync(Guid doctorId, [Refit.Query] DateTime? startDate = null, [Refit.Query] DateTime? endDate = null);

        /// <summary>
        /// 更新诊疗状态
        /// </summary>
        [Refit.Post("/api/v1/consultation/{id}/update-status")]
        Task<Refit.ApiResponse<ConsultationDetailDto>> UpdateStatusAsync(Guid id, [Refit.Body] UpdateStatusDto dto);

        /// <summary>
        /// 删除诊疗记录（软删除）
        /// </summary>
        [Refit.Delete("/api/v1/consultation/{id}")]
        Task<Refit.ApiResponse<object>> DeleteAsync(Guid id);
    }
}
