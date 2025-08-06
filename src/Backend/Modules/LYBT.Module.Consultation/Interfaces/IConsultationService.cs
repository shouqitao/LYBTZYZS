using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Module.Consultation.Interfaces
{
    /// <summary>
    /// 看诊服务接口（替代IDiagnosisTreatmentService）
    /// </summary>
    public interface IConsultationService
    {
        /// <summary>
        /// 分页查询看诊记录
        /// </summary>
        Task<PagedResult<ConsultationDto>> GetPagedAsync(ConsultationPagedQueryDto query);

        /// <summary>
        /// 获取看诊详情
        /// </summary>
        Task<ConsultationDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 根据医疗案例ID获取看诊信息
        /// </summary>
        Task<ConsultationDetailDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 开始看诊
        /// </summary>
        Task<ConsultationDetailDto> StartConsultationAsync(ConsultationStartDto dto);

        /// <summary>
        /// 更新看诊信息
        /// </summary>
        Task<ConsultationDetailDto> UpdateConsultationAsync(Guid id, ConsultationUpdateDto dto);

        /// <summary>
        /// 完成看诊
        /// </summary>
        Task<bool> CompleteConsultationAsync(Guid id, ConsultationCompleteDto dto);

        /// <summary>
        /// 获取医生今日看诊列表
        /// </summary>
        Task<List<ConsultationDto>> GetTodayConsultationsByDoctorAsync(Guid doctorId);

        /// <summary>
        /// 获取患者历史看诊记录
        /// </summary>
        Task<List<ConsultationDto>> GetPatientHistoryAsync(Guid patientId);

        /// <summary>
        /// 统计医生看诊数量
        /// </summary>
        Task<int> GetDoctorConsultationCountAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 删除看诊记录（软删除）
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}