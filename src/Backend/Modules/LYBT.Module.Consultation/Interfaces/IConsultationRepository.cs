using LYBT.Models.Consultation;

namespace LYBT.Module.Consultation.Interfaces
{
    /// <summary>
    /// 看诊仓储接口（替代IDiagnosisTreatmentRepository）
    /// </summary>
    public interface IConsultationRepository
    {
        /// <summary>
        /// 获取所有看诊记录
        /// </summary>
        Task<List<ConsultationModel>> GetListAsync();

        /// <summary>
        /// 根据ID获取看诊记录
        /// </summary>
        Task<ConsultationModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建看诊记录
        /// </summary>
        Task<ConsultationModel> CreateAsync(ConsultationModel model);

        /// <summary>
        /// 更新看诊记录
        /// </summary>
        Task<bool> UpdateAsync(ConsultationModel model);

        /// <summary>
        /// 删除看诊记录
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 根据医疗案例ID获取看诊记录
        /// </summary>
        Task<ConsultationModel?> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 根据患者ID获取看诊历史
        /// </summary>
        Task<List<ConsultationModel>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据医生ID获取看诊记录
        /// </summary>
        Task<List<ConsultationModel>> GetByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 根据日期范围获取看诊记录
        /// </summary>
        Task<List<ConsultationModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}