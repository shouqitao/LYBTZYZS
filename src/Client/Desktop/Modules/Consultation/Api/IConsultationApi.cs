using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Modules.Consultation.Api
{
    /// <summary>
    /// 看诊API客户端接口 - UltraThink统一标准
    /// </summary>
    public interface IConsultationApi
    {
        /// <summary>
        /// 分页查询看诊记录
        /// </summary>
        [Get("/api/v1/consultation")]
        Task<Refit.ApiResponse<PagedResult<ConsultationDto>>> GetConsultationsAsync(
            [Query] int page = 1,
            [Query] int pageSize = 10,
            [Query] string? keyword = null,
            [Query] Guid? doctorId = null,
            [Query] Guid? patientId = null,
            [Query] DateTime? startDate = null,
            [Query] DateTime? endDate = null,
            [Query] int? status = null);

        /// <summary>
        /// 获取看诊详情
        /// </summary>
        [Get("/api/v1/consultation/{id}")]
        Task<Refit.ApiResponse<ConsultationDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 根据医疗案例ID获取看诊信息
        /// </summary>
        [Get("/api/v1/consultation/medical-case/{medicalCaseId}")]
        Task<Refit.ApiResponse<ConsultationDetailDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 开始看诊
        /// </summary>
        [Post("/api/v1/consultation/start")]
        Task<Refit.ApiResponse<ConsultationDetailDto>> StartConsultationAsync([Body] ConsultationStartDto dto);

        /// <summary>
        /// 更新看诊信息
        /// </summary>
        [Put("/api/v1/consultation/{id}")]
        Task<Refit.ApiResponse<ConsultationDetailDto>> UpdateConsultationAsync(Guid id, [Body] ConsultationUpdateDto dto);

        /// <summary>
        /// 完成看诊
        /// </summary>
        [Post("/api/v1/consultation/{id}/complete")]
        Task<Refit.ApiResponse<object>> CompleteConsultationAsync(Guid id, [Body] ConsultationCompleteDto dto);

        /// <summary>
        /// 取消看诊
        /// </summary>
        [Post("/api/v1/consultation/{id}/cancel")]
        Task<Refit.ApiResponse<object>> CancelConsultationAsync(Guid id, [Body] string reason);

        /// <summary>
        /// 获取统计信息
        /// </summary>
        [Get("/api/v1/consultation/statistics")]
        Task<Refit.ApiResponse<object>> GetStatisticsAsync(
            [Query] DateTime? startDate = null,
            [Query] DateTime? endDate = null);

        /// <summary>
        /// 获取医生今日看诊列表
        /// </summary>
        [Get("/api/v1/consultation/doctor/{doctorId}/today")]
        Task<Refit.ApiResponse<List<ConsultationDto>>> GetTodayConsultationsByDoctorAsync(Guid doctorId);

        /// <summary>
        /// 获取患者历史看诊记录
        /// </summary>
        [Get("/api/v1/consultation/patient/{patientId}/history")]
        Task<Refit.ApiResponse<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId);

        /// <summary>
        /// 统计医生看诊数量
        /// </summary>
        [Get("/api/v1/consultation/doctor/{doctorId}/count")]
        Task<Refit.ApiResponse<int>> GetDoctorConsultationCountAsync(Guid doctorId, [Query] DateTime? startDate = null, [Query] DateTime? endDate = null);

        /// <summary>
        /// 更新看诊状态
        /// </summary>
        [Post("/api/v1/consultation/{id}/update-status")]
        Task<Refit.ApiResponse<ConsultationDetailDto>> UpdateStatusAsync(Guid id, [Body] UpdateStatusDto dto);

        /// <summary>
        /// 删除看诊记录（软删除）
        /// </summary>
        [Delete("/api/v1/consultation/{id}")]
        Task<Refit.ApiResponse<object>> DeleteAsync(Guid id);
    }
}