using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Services.Session;

/// <summary>
/// 统一的会话管理服务接口
/// Phase 2重构：整合ISessionManager和IUserSessionManager，消除职责重叠
/// 提供完整的用户、患者、诊疗会话管理功能
/// </summary>
public interface IUnifiedSessionManager
{
    #region 用户会话管理
    
    /// <summary>
    /// 当前登录用户
    /// </summary>
    UserDto? CurrentUser { get; }
    
    /// <summary>
    /// 是否已登录
    /// </summary>
    bool IsLoggedIn { get; }
    
    /// <summary>
    /// 登录时间
    /// </summary>
    DateTime? LoginTime { get; }
    
    /// <summary>
    /// JWT令牌
    /// </summary>
    string? Token { get; }
    
    /// <summary>
    /// 设置用户会话
    /// </summary>
    void SetUserSession(UserDto user, string token);
    
    /// <summary>
    /// 清除用户会话
    /// </summary>
    void ClearUserSession();
    
    /// <summary>
    /// 刷新用户信息
    /// </summary>
    void RefreshUserInfo(UserDto user);
    
    #endregion
    
    #region 权限管理
    
    /// <summary>
    /// 获取当前用户角色
    /// </summary>
    UserRole? GetUserRole();
    
    /// <summary>
    /// 检查是否有指定角色
    /// </summary>
    bool HasRole(UserRole role);
    
    /// <summary>
    /// 检查是否有指定权限
    /// </summary>
    bool HasPermission(string permission);
    
    /// <summary>
    /// 是否有管理权限
    /// </summary>
    bool HasManagementAccess();
    
    /// <summary>
    /// 是否有医疗权限
    /// </summary>
    bool HasMedicalAccess();
    
    /// <summary>
    /// 获取可访问的模块列表
    /// </summary>
    IEnumerable<string> GetAccessibleModules();
    
    #endregion
    
    #region 患者会话管理
    
    /// <summary>
    /// 当前选中的患者
    /// </summary>
    PatientDto? CurrentPatient { get; set; }
    
    /// <summary>
    /// 选择患者
    /// </summary>
    void SelectPatient(PatientDto? patient);
    
    /// <summary>
    /// 清除患者选择
    /// </summary>
    void ClearPatientSelection();
    
    #endregion
    
    #region 诊疗会话管理
    
    /// <summary>
    /// 当前活跃的诊疗会话
    /// </summary>
    ConsultationDto? ActiveConsultation { get; set; }
    
    /// <summary>
    /// 当前医案ID
    /// </summary>
    Guid? CurrentMedicalCaseId { get; set; }
    
    /// <summary>
    /// 诊疗状态
    /// </summary>
    ConsultationStatus ConsultationStatus { get; }
    
    /// <summary>
    /// 是否有活跃的诊疗会话
    /// </summary>
    bool HasActiveConsultation { get; }
    
    /// <summary>
    /// 开始诊疗会话
    /// </summary>
    void StartConsultation(PatientDto patient, Guid? medicalCaseId = null);
    
    /// <summary>
    /// 结束诊疗会话
    /// </summary>
    void EndConsultation();
    
    /// <summary>
    /// 更新诊疗状态
    /// </summary>
    void UpdateConsultationStatus(ConsultationStatus status);
    
    #endregion
    
    #region 统一事件系统
    
    /// <summary>
    /// 用户会话变化事件
    /// </summary>
    event EventHandler<UserSessionChangedEventArgs>? UserSessionChanged;
    
    /// <summary>
    /// 患者选择变化事件
    /// </summary>
    event EventHandler<PatientSelectionChangedEventArgs>? PatientSelectionChanged;
    
    /// <summary>
    /// 诊疗会话变化事件
    /// </summary>
    event EventHandler<ConsultationSessionChangedEventArgs>? ConsultationSessionChanged;
    
    /// <summary>
    /// 权限变化事件
    /// </summary>
    event EventHandler<PermissionChangedEventArgs>? PermissionChanged;
    
    /// <summary>
    /// 全局状态消息事件
    /// </summary>
    event EventHandler<SessionMessageEventArgs>? SessionMessage;
    
    #endregion
    
    #region 会话生命周期
    
    /// <summary>
    /// 重置所有会话状态
    /// </summary>
    void ResetAll();
    
    /// <summary>
    /// 保存会话状态（用于持久化）
    /// </summary>
    SessionState GetSessionState();
    
    /// <summary>
    /// 恢复会话状态（从持久化）
    /// </summary>
    void RestoreSessionState(SessionState state);
    
    #endregion
}

#region 事件参数定义

/// <summary>
/// 用户会话变化事件参数
/// </summary>
public class UserSessionChangedEventArgs : EventArgs
{
    public UserDto? PreviousUser { get; set; }
    public UserDto? CurrentUser { get; set; }
    public SessionChangeReason Reason { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 患者选择变化事件参数
/// </summary>
public class PatientSelectionChangedEventArgs : EventArgs
{
    public PatientDto? PreviousPatient { get; set; }
    public PatientDto? CurrentPatient { get; set; }
    public SelectionChangeReason Reason { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 诊疗会话变化事件参数
/// </summary>
public class ConsultationSessionChangedEventArgs : EventArgs
{
    public ConsultationDto? PreviousConsultation { get; set; }
    public ConsultationDto? CurrentConsultation { get; set; }
    public ConsultationStatus? PreviousStatus { get; set; }
    public ConsultationStatus? CurrentStatus { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 权限变化事件参数
/// </summary>
public class PermissionChangedEventArgs : EventArgs
{
    public UserRole? PreviousRole { get; set; }
    public UserRole? CurrentRole { get; set; }
    public IEnumerable<string> AddedPermissions { get; set; } = new List<string>();
    public IEnumerable<string> RemovedPermissions { get; set; } = new List<string>();
    public DateTime OccurredAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 会话消息事件参数
/// </summary>
public class SessionMessageEventArgs : EventArgs
{
    public string Message { get; set; } = "";
    public MessageSeverity Severity { get; set; }
    public string? Source { get; set; }
    public Dictionary<string, object>? Context { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.Now;
}

#endregion

#region 枚举定义

/// <summary>
/// 会话变化原因
/// </summary>
public enum SessionChangeReason
{
    Login,
    Logout,
    Timeout,
    Refresh,
    ForceLogout,
    UserInfoUpdated
}

/// <summary>
/// 选择变化原因
/// </summary>
public enum SelectionChangeReason
{
    UserSelection,
    SystemSelection,
    WorkflowTransition,
    Clear
}

/// <summary>
/// 消息严重程度
/// </summary>
public enum MessageSeverity
{
    Info,
    Warning,
    Error,
    Success
}

/// <summary>
/// 诊疗状态（统一定义）
/// </summary>
public enum ConsultationStatus
{
    NotStarted,
    InProgress,
    Paused,
    Completed,
    Cancelled
}

#endregion

#region 会话状态模型

/// <summary>
/// 会话状态快照（用于持久化）
/// </summary>
public class SessionState
{
    public UserDto? User { get; set; }
    public string? Token { get; set; }
    public DateTime? LoginTime { get; set; }
    public PatientDto? Patient { get; set; }
    public ConsultationDto? Consultation { get; set; }
    public Guid? MedicalCaseId { get; set; }
    public ConsultationStatus Status { get; set; }
    public Dictionary<string, object> ExtendedData { get; set; } = new();
    public DateTime CapturedAt { get; set; } = DateTime.Now;
}

#endregion