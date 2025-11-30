using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Module.Consultations.Interfaces
{
    /// <summary>
    /// 诊疗服务接口 - Read Layer（Issue #1600 Phase 3）
    /// 职责：提供诊疗记录的只读查询功能
    /// 所有Write操作必须通过IMedicalCaseService聚合根进行
    /// </summary>
    public interface IConsultationService
    {
        /// <summary>
        /// 根据ID获取诊疗详情
        /// </summary>
        Task<Result<ConsultationDto>> GetByIdAsync(Guid id);

        // ========== Write方法已移除（Issue #1600 Phase 1）==========
        // CreateAsync, UpdateAsync, DeleteAsync 已移除
        // 所有写操作必须通过MedicalCase聚合根进行

        /// <summary>
        /// 根据医案ID获取诊疗记录列表
        /// </summary>
        Task<Result<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        // ========== Write方法已移除（Issue #1600 Phase 3）==========
        // CompleteStep1Async 已移除，迁移至IMedicalCaseService
        // 所有写操作必须通过MedicalCase聚合根进行

        // Issue #1562 Phase 1: 已删除 StartAsync（工作流启动方法）
        // Issue #1562 Phase 1: 已删除 GetStatisticsAsync（统计功能属于过度设计）
    }
}
