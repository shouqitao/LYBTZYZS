using LYBT.Entities.Consultation;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.Consultation.Interfaces
{
    /// <summary>
    /// 看诊仓储接口 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，扩展看诊特定业务方法
    /// </summary>
    public interface IConsultationRepository : IBaseRepository<ConsultationModel>
    {
        // 注意：基础CRUD方法由IBaseRepository提供
        // 这里只定义看诊特有的业务方法

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