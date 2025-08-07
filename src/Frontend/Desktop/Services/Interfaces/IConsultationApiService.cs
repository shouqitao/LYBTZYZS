using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 看诊API服务接口
    /// </summary>
    public interface IConsultationApiService
    {
        /// <summary>
        /// 分页查询看诊记录
        /// </summary>
        [Post("/api/v1/consultation/paged")]
        Task<Refit.ApiResponse<PagedResult<ConsultationDto>>> GetPagedAsync([Body] ConsultationPagedQueryDto query);

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
        /// 删除看诊记录（软删除）
        /// </summary>
        [Delete("/api/v1/consultation/{id}")]
        Task<Refit.ApiResponse<object>> DeleteAsync(Guid id);
    }
}