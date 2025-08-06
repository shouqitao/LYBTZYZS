using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Module.Consultation.Interfaces
{
    /// <summary>
    /// 看诊服务接口（替代IDiagnosisTreatmentService）
    /// </summary>
    public interface IConsultationService
    {
        /// <summary>
        /// 获取看诊列表
        /// </summary>
        Task<List<ConsultationDto>> GetListAsync();

        /// <summary>
        /// 分页获取看诊列表
        /// </summary>
        Task<PaginatedResult<ConsultationDto>> GetPagedAsync(PaginationRequest request);

        /// <summary>
        /// 获取看诊详情
        /// </summary>
        Task<ConsultationDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建看诊记录
        /// </summary>
        Task<ConsultationDetailDto> CreateAsync(ConsultationCreateDto dto);

        /// <summary>
        /// 更新看诊记录
        /// </summary>
        Task<bool> UpdateAsync(Guid id, ConsultationUpdateDto dto);

        /// <summary>
        /// 删除看诊记录（软删除）
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 根据医疗案例ID获取看诊记录
        /// </summary>
        Task<ConsultationDetailDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 根据患者ID获取看诊历史
        /// </summary>
        Task<List<ConsultationDto>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据医生ID获取看诊记录
        /// </summary>
        Task<List<ConsultationDto>> GetByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 获取今日看诊列表
        /// </summary>
        Task<List<ConsultationDto>> GetTodayConsultationsAsync();

        /// <summary>
        /// 完成看诊
        /// </summary>
        Task<bool> CompleteConsultationAsync(Guid id);
    }
}