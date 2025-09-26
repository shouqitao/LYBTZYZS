using LYBT.Infrastructure.Interfaces;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;

namespace LYBT.Module.Consultation.Interfaces
{
    /// <summary>
    /// 诊疗仓储接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IConsultationRepository : IRepository<ConsultationEntity>
    {
        /// <summary>
        /// 根据患者ID获取诊疗记录
        /// </summary>
        Task<List<ConsultationEntity>> GetByPatientIdAsync(Guid patientId);
    }
}