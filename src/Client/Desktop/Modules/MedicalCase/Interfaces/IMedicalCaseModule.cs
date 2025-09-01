using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// 医案模块接口 - UltraThink三层架构模块层
/// 职责：统一模块入口，事件管理，模块间协调
/// </summary>
public interface IMedicalCaseModule : IMedicalCaseService, IDisposable
{
    #region 事件定义

    /// <summary>
    /// 医案状态变更事件
    /// </summary>
    event EventHandler<MedicalCaseStatusChangedEventArgs>? MedicalCaseStatusChanged;

    /// <summary>
    /// 医案操作事件
    /// </summary>
    event EventHandler<MedicalCaseOperationEventArgs>? MedicalCaseOperation;

    /// <summary>
    /// 诊疗流程事件
    /// </summary>
    event EventHandler<ConsultationWorkflowEventArgs>? ConsultationWorkflow;

    #endregion

    #region 模块特定方法

    /// <summary>
    /// 获取医案统计摘要
    /// </summary>
    Task<ServiceResult<MedicalCaseStatisticsSummaryDto>> GetStatisticsSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取患者医案历史统计
    /// </summary>
    Task<ServiceResult<PatientMedicalCaseStatDto>> GetPatientMedicalCaseStatAsync(Guid patientId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取医生工作统计
    /// </summary>
    Task<ServiceResult<DoctorMedicalCaseStatisticsDto>> GetDoctorMedicalCaseStatisticsAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 批量更新医案状态
    /// </summary>
    Task<ServiceResult<MedicalCaseBatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> medicalCaseIds, MedicalCaseStatus status);

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
}

/// <summary>
/// 医案状态变更事件参数
/// </summary>
public class MedicalCaseStatusChangedEventArgs : EventArgs
{
    public Guid MedicalCaseId { get; set; }
    public MedicalCaseStatus OldStatus { get; set; }
    public MedicalCaseStatus NewStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
}

/// <summary>
/// 医案操作事件参数
/// </summary>
public class MedicalCaseOperationEventArgs : EventArgs
{
    public Guid MedicalCaseId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string OperationDetails { get; set; } = string.Empty;
    public DateTime OperatedAt { get; set; }
    public Guid OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 诊疗流程事件参数
/// </summary>
public class ConsultationWorkflowEventArgs : EventArgs
{
    public Guid MedicalCaseId { get; set; }
    public string WorkflowStep { get; set; } = string.Empty;
    public string StepDetails { get; set; } = string.Empty;
    public DateTime StepExecutedAt { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}