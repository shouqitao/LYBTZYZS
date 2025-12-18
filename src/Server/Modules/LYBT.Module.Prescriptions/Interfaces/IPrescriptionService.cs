using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Interfaces
{
    /// <summary>
    /// 处方服务接口 - 简化版，包含基础CRUD功能
    /// </summary>
    public interface IPrescriptionService
    {
        /// <summary>
        /// 根据ID获取处方详情
        /// </summary>
        Task<Result<PrescriptionDetailDto>> GetByIdAsync(Guid id);

        // ========== Write方法已移除（Issue #1601 Phase 1）==========
        // CreateAsync, UpdateAsync, DeleteAsync, PhysicalDeleteAsync 已移除
        // 所有写操作必须通过MedicalCase聚合根进行

        /// <summary>
        /// 根据病例ID获取处方列表
        /// </summary>
        Task<Result<List<PrescriptionDetailDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        // ========== Clone/Import方法已移除（Issue #1601 Phase 1）==========
        // CloneAsync, ClonePrescriptionAsync, ImportFormulaIntoPrescriptionAsync 已移除
        // 所有写操作必须通过MedicalCase聚合根进行

        // ========== 跨医案查询方法已迁移（OpenSpec: consolidate-medicalcase-queries）==========
        // SearchPrescriptionsAsync 已删除 - 请使用 GET /api/v1/medicalcases/search
        // GetPatientRecentPrescriptionsAsync 已删除 - 请使用 GET /api/v1/medicalcases/patient/{patientId}/recent
    }
}
