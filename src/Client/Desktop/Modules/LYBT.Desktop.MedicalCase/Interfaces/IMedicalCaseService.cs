using LYBT.Desktop.Contracts.Services;

namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 病案Service接口 - 聚合根门面模式
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    /// OpenSpec: simplify-medicalcase-api - 聚合根统一管理Consultation和Prescription
    /// OpenSpec: refactor-frontend-srp-patterns (ADR-1) - 门面接口，聚合Query/Command/Lifecycle职责
    /// </summary>
    /// <remarks>
    /// 继承自三个专职接口实现SRP职责分离：
    /// - IMedicalCaseQueryService: 查询职责
    /// - IMedicalCaseCommandService: 命令职责
    /// - IMedicalCaseLifecycleService: 生命周期职责
    /// </remarks>
    public interface IMedicalCaseService :
        IMedicalCaseQueryService,
        IMedicalCaseCommandService,
        IMedicalCaseLifecycleService
    {
        // OpenSpec: cleanup-medicalcase-dead-code - 以下方法已删除（0调用，功能由SaveAsync替代）
        // - UpdateConsultation: 直接修改Current.Consultation即可
        // - CreatePrescriptionAsync: 通过SaveAsync创建
        // - DeletePrescriptionAsync: 通过SaveAsync设置NeedsPrescription=false触发

        // 所有成员通过接口继承获得：
        // Query: GetPagedAsync, QueryAsync, GetUnfinishedCaseByPatientIdAsync, CloseCaseAsync
        // Command: Current, HasChanges, SaveAsync, DeleteAsync, CreateMedicalCaseAsync
        // Lifecycle: MedicalCaseId, CurrentConsultation, CurrentPrescription, InitializeAsync, ReloadAsync,
        //            SaveDraftAsync, CancelMedicalCaseAsync, CompleteMedicalCaseAsync, ResumeDraftAsync
    }
}
