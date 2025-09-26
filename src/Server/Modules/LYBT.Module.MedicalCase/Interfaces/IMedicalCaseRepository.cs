using LYBT.Infrastructure.Interfaces;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;

namespace LYBT.Module.MedicalCase.Interfaces
{
    /// <summary>
    /// 医疗案例仓储接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IMedicalCaseRepository : IRepository<MedicalCaseEntity>
    {
        /// <summary>
        /// 根据患者ID获取医疗案例
        /// </summary>
        Task<List<MedicalCaseEntity>> GetByPatientIdAsync(Guid patientId);
    }
}