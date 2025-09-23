using System;
using Prism.Events;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Events;

/// <summary>
/// 统一事件定义 - 合并重复事件，保持单一事件源
/// 分层纯净化重构：删除重复定义，统一命名空间
/// </summary>

#region 认证事件

/// <summary>
/// 登录成功事件
/// </summary>
public class LoginSuccessEvent : PubSubEvent<LoginSuccessEventArgs> { }

public class LoginSuccessEventArgs
{
    public UserDto User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 登出事件
/// </summary>
public class LogoutEvent : PubSubEvent<LogoutEventArgs> { }

public class LogoutEventArgs
{
    public string? Reason { get; set; }
    public DateTime LogoutTime { get; set; } = DateTime.Now;
}

#endregion

#region 患者事件

/// <summary>
/// 患者选中事件 - 统一定义
/// </summary>
public class PatientSelectedEvent : PubSubEvent<PatientSelectedEventArgs> { }

public class PatientSelectedEventArgs
{
    public PatientDto Patient { get; set; } = null!;
    public string? Source { get; set; }
    public DateTime SelectedTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 患者更新事件
/// </summary>
public class PatientUpdatedEvent : PubSubEvent<PatientUpdatedEventArgs> { }

public class PatientUpdatedEventArgs
{
    public PatientDto Patient { get; set; } = null!;
    public string UpdateType { get; set; } = string.Empty; // Add, Update, Delete
}

#endregion

#region 病历事件

/// <summary>
/// 病历选中事件
/// </summary>
public class MedicalCaseSelectedEvent : PubSubEvent<MedicalCaseSelectedEventArgs> { }

public class MedicalCaseSelectedEventArgs
{
    public MedicalCaseDto MedicalCase { get; set; } = null!;
    public PatientDto? Patient { get; set; }
}

/// <summary>
/// 病历状态变更事件
/// </summary>
public class MedicalCaseStatusChangedEvent : PubSubEvent<MedicalCaseStatusChangedEventArgs> { }

public class MedicalCaseStatusChangedEventArgs
{
    public int MedicalCaseId { get; set; }
    public MedicalCaseStatus OldStatus { get; set; }
    public MedicalCaseStatus NewStatus { get; set; }
    public string? Reason { get; set; }
}

#endregion

#region 问诊事件

/// <summary>
/// 问诊开始事件 - 统一定义
/// </summary>
public class ConsultationStartedEvent : PubSubEvent<ConsultationStartedEventArgs> { }

public class ConsultationStartedEventArgs
{
    public int ConsultationId { get; set; }
    public int MedicalCaseId { get; set; }
    public PatientDto Patient { get; set; } = null!;
    public DateTime StartTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 问诊完成事件 - 统一定义
/// </summary>
public class ConsultationCompletedEvent : PubSubEvent<ConsultationCompletedEventArgs> { }

public class ConsultationCompletedEventArgs
{
    public int ConsultationId { get; set; }
    public ConsultationDto Consultation { get; set; } = null!;
    public DateTime CompletedTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 四诊数据保存事件
/// </summary>
public class FourDiagnosisSavedEvent : PubSubEvent<FourDiagnosisSavedEventArgs> { }

public class FourDiagnosisSavedEventArgs
{
    public int ConsultationId { get; set; }
    public string DiagnosisType { get; set; } = string.Empty; // Inspection, Auscultation, Inquiry, Palpation
    public object DiagnosisData { get; set; } = null!;
}

#endregion

#region 处方事件

/// <summary>
/// 处方保存事件 - 统一定义
/// </summary>
public class PrescriptionSavedEvent : PubSubEvent<PrescriptionSavedEventArgs> { }

public class PrescriptionSavedEventArgs
{
    public int PrescriptionId { get; set; }
    public PrescriptionDto Prescription { get; set; } = null!;
    public bool IsNew { get; set; }
}

/// <summary>
/// 处方变更事件
/// </summary>
public class PrescriptionChangedEvent : PubSubEvent<PrescriptionChangedEventArgs> { }

public class PrescriptionChangedEventArgs
{
    public int PrescriptionId { get; set; }
    public string ChangeType { get; set; } = string.Empty; // HerbAdded, HerbRemoved, DosageChanged, etc.
    public object? ChangeData { get; set; }
}

#endregion

#region 导航事件

/// <summary>
/// 导航请求事件
/// </summary>
public class NavigationRequestEvent : PubSubEvent<NavigationRequestEventArgs> { }

public class NavigationRequestEventArgs
{
    public string ViewName { get; set; } = string.Empty;
    public string? RegionName { get; set; }
    public object? Parameters { get; set; }
}

/// <summary>
/// 工作流步骤导航事件
/// </summary>
public class WorkflowStepNavigationEvent : PubSubEvent<WorkflowStepNavigationEventArgs> { }

public class WorkflowStepNavigationEventArgs
{
    public WorkflowStep CurrentStep { get; set; }
    public WorkflowStep? NextStep { get; set; }
    public bool CanNavigate { get; set; } = true;
}

#endregion

#region 数据刷新事件

/// <summary>
/// 数据刷新请求事件
/// </summary>
public class DataRefreshRequestEvent : PubSubEvent<DataRefreshRequestEventArgs> { }

public class DataRefreshRequestEventArgs
{
    public string DataType { get; set; } = string.Empty; // Patient, MedicalCase, Consultation, etc.
    public int? EntityId { get; set; }
    public bool ForceRefresh { get; set; }
}

#endregion

#region 通知事件

/// <summary>
/// 状态消息事件
/// </summary>
public class StatusMessageEvent : PubSubEvent<StatusMessageEventArgs> { }

public class StatusMessageEventArgs
{
    public string Message { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Info;
    public int? Duration { get; set; } // 显示时长（毫秒）
}

public enum MessageType
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// 错误发生事件
/// </summary>
public class ErrorOccurredEvent : PubSubEvent<ErrorOccurredEventArgs> { }

public class ErrorOccurredEventArgs
{
    public string ErrorMessage { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public string? Source { get; set; }
    public bool IsCritical { get; set; }
}

#endregion

#region 已废弃事件标记

// 以下事件已废弃，将在下个版本删除
// - ConsultationSessionStartedEvent (使用ConsultationStartedEvent)
// - TCMFourDiagnosisCompletedEvent (使用FourDiagnosisSavedEvent)
// - PrescriptionCreatedEvent (使用PrescriptionSavedEvent)
// - ConsultationNavigationEvent (使用NavigationRequestEvent)
// - DiagnosisSavedEvent (使用FourDiagnosisSavedEvent)
// - ImportHistoryDataEvent (使用DataRefreshRequestEvent)
// - ConsultationDataUpdatedEvent (使用DataRefreshRequestEvent)
// - ModuleNavigationEvent (使用NavigationRequestEvent)
// - 重复的事件定义已从其他文件移除

#endregion