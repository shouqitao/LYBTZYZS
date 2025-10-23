using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// 医案查询服务接口
    /// Epic #1583 - Phase 2: 智能路由（避免Patients↔MedicalCase循环依赖）
    /// </summary>
    public interface IMedicalCaseQueryService
    {
        /// <summary>
        /// 查询患者是否有未完成医案
        /// Phase 2: 临时使用GetByPatientIdAsync过滤Status=Active的医案
        /// Phase 5: 优化为专用API /api/medicalcases/patient/{patientId}/unfinished
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>未完成的医案，如果没有则返回null</returns>
        Task<MedicalCaseDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 关闭医案（级联删除Consultation和Prescription）
        /// Phase 2: 临时使用DeleteAsync（不符合业务语义）
        /// Phase 5: 实现专用API /api/medicalcases/{id}/close
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <returns>是否成功关闭</returns>
        Task<bool> CloseAsync(Guid medicalCaseId);
    }
}
