using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCases.Interfaces
{
    /// <summary>
    /// 病案查询服务接口 - 读操作
    /// Phase 3: 从IMedicalCaseService拆分，遵循CQRS原则
    /// 职责：GetById, GetList, Search等查询操作
    /// </summary>
    public interface IMedicalCaseQueryService
    {
        /// <summary>
        /// 根据ID获取病案详情
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <returns>病案实体（包含完整关联数据）</returns>
        Task<MedicalCase?> GetByIdAsync(Guid id);

        /// <summary>
        /// 查询病案列表（分页）
        /// </summary>
        /// <param name="status">病案状态（可选）</param>
        /// <param name="patientId">患者ID（可选）</param>
        /// <param name="page">页码（从1开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>分页结果</returns>
        Task<PagedResult<MedicalCase>> GetListAsync(
            MedicalCaseStatus? status,
            Guid? patientId,
            int page,
            int pageSize);

        /// <summary>
        /// 查询辨证记录列表
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <returns>辨证记录DTO列表</returns>
        Task<List<ConsultationDto>> GetConsultationListAsync(Guid medicalCaseId);

        /// <summary>
        /// 查询处方列表
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <returns>处方DTO列表</returns>
        Task<List<PrescriptionDto>> GetPrescriptionListAsync(Guid medicalCaseId);

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="doctorId">医生ID（为Guid.Empty时不筛选医生）</param>
        /// <returns>未完成的病案实体（包含关联数据），若无则返回null</returns>
        Task<MedicalCase?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId);

        /// <summary>
        /// 获取待看诊队列（Status = Active的医案患者列表）
        /// </summary>
        /// <param name="doctorId">医生ID</param>
        /// <returns>待诊队列列表</returns>
        Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId);

        /// <summary>
        /// 获取所有待看诊队列（管理员专用）
        /// </summary>
        /// <returns>待诊队列列表</returns>
        Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync();
    }
}
