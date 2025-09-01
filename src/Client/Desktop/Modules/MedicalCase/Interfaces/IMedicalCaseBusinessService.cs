using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// 医案业务服务接口 - UltraThink三层架构业务层
/// 职责：业务流程编排、工作流管理、事件处理、复杂业务逻辑
/// </summary>
public interface IMedicalCaseBusinessService
{
    #region 医案CRUD业务操作

    /// <summary>
    /// 创建医案业务流程
    /// </summary>
    Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto createDto);

    /// <summary>
    /// 更新医案业务流程
    /// </summary>
    Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto updateDto);

    /// <summary>
    /// 删除医案业务流程
    /// </summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id);

    #endregion

    #region 医案状态管理

    /// <summary>
    /// 更新医案状态业务流程
    /// </summary>
    Task<ServiceResult<bool>> UpdateStatusAsync(Guid medicalCaseId, MedicalCaseStatus status, string reason = "");

    /// <summary>
    /// 批量更新医案状态
    /// </summary>
    Task<ServiceResult<MedicalCaseBatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> medicalCaseIds, MedicalCaseStatus status);

    #endregion

    #region 诊疗流程管理

    /// <summary>
    /// 获取诊疗流程状态
    /// </summary>
    Task<ServiceResult<ConsultationWorkflowStatusDto>> GetConsultationWorkflowStatusAsync(Guid medicalCaseId);

    /// <summary>
    /// 开始看诊流程
    /// </summary>
    Task<ServiceResult<bool>> StartConsultationWorkflowAsync(Guid medicalCaseId);

    /// <summary>
    /// 完成看诊流程
    /// </summary>
    Task<ServiceResult<bool>> CompleteConsultationWorkflowAsync(Guid medicalCaseId, string completionNotes);

    /// <summary>
    /// 暂停看诊流程
    /// </summary>
    Task<ServiceResult<bool>> PauseConsultationWorkflowAsync(Guid medicalCaseId, string pauseReason);

    /// <summary>
    /// 恢复看诊流程
    /// </summary>
    Task<ServiceResult<bool>> ResumeConsultationWorkflowAsync(Guid medicalCaseId);

    #endregion

    #region 高级业务操作

    /// <summary>
    /// 医案数据同步
    /// </summary>
    Task<ServiceResult<bool>> SyncMedicalCaseDataAsync(Guid medicalCaseId);

    /// <summary>
    /// 医案数据归档
    /// </summary>
    Task<ServiceResult<bool>> ArchiveMedicalCaseAsync(Guid medicalCaseId, string archiveReason);

    /// <summary>
    /// 医案数据恢复
    /// </summary>
    Task<ServiceResult<bool>> RestoreMedicalCaseAsync(Guid medicalCaseId);

    #endregion

    #region 业务事件

    /// <summary>
    /// 触发医案状态变更事件
    /// </summary>
    event EventHandler<MedicalCaseStatusChangedEventArgs>? MedicalCaseStatusChanged;

    /// <summary>
    /// 触发医案操作事件
    /// </summary>
    event EventHandler<MedicalCaseOperationEventArgs>? MedicalCaseOperation;

    /// <summary>
    /// 触发诊疗工作流事件
    /// </summary>
    event EventHandler<ConsultationWorkflowEventArgs>? ConsultationWorkflow;

    #endregion
}