using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Core.Interfaces.Services;

/// <summary>
/// 会话管理服务接口 - 替代Redux的轻量级状态管理解决方案
/// 采用UltraThink架构标准，提供统一的用户登录、患者选择和诊疗会话管理
/// 支持事件驱动的状态通知机制，保证UI与业务状态同步
/// </summary>
public interface ISessionManager
{
    #region 当前会话状态

    /// <summary>
    /// 获取或设置当前选中的患者信息
    /// </summary>
    /// <value>患者详细信息，如果未选择患者则为 null</value>
    PatientDto? CurrentPatient { get; set; }

    /// <summary>
    /// 获取或设置当前活跃的诊疗会话
    /// </summary>
    /// <value>诊疗会话信息，如果没有活跃诊疗则为 null</value>
    ConsultationDto? ActiveConsultation { get; set; }

    /// <summary>
    /// 获取或设置当前登录的用户信息
    /// </summary>
    /// <value>用户信息，如果未登录则为 null</value>
    UserDto? CurrentUser { get; set; }

    /// <summary>
    /// 获取或设置当前医疗案例的唯一标识符
    /// </summary>
    /// <value>医疗案例ID，如果没有活跃案例则为 null</value>
    Guid? CurrentMedicalCaseId { get; set; }

    /// <summary>
    /// 获取或设置当前诊疗的进行状态
    /// </summary>
    /// <value>诊疗状态枚举值</value>
    ConsultationStatus ConsultationStatus { get; set; }

    #endregion

    #region 状态变化事件

    /// <summary>
    /// 当患者选择发生更改时引发此事件
    /// </summary>
    event EventHandler<PatientChangedEventArgs>? PatientChanged;

    /// <summary>
    /// 当诊疗状态发生更改时引发此事件
    /// </summary>
    event EventHandler<ConsultationChangedEventArgs>? ConsultationChanged;

    /// <summary>
    /// 当用户登录状态发生更改时引发此事件
    /// </summary>
    event EventHandler<UserChangedEventArgs>? UserChanged;

    /// <summary>
    /// 当需要显示全局状态消息时引发此事件
    /// </summary>
    event EventHandler<StatusMessageEventArgs>? StatusMessage;

    #endregion

    #region 会话管理方法

    /// <summary>
    /// 开始新的诊疗会话
    /// </summary>
    /// <param name="patient">要开始诊疗的患者信息</param>
    /// <param name="medicalCaseId">可选的医疗案例ID，如果未提供将自动生成</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="patient"/> 为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">当会话操作失败时抛出</exception>
    void StartConsultation(PatientDto patient, Guid? medicalCaseId = null);

    /// <summary>
    /// 结束当前的诊疗会话
    /// </summary>
    /// <exception cref="InvalidOperationException">当会话操作失败时抛出</exception>
    void EndConsultation();

    /// <summary>
    /// 设置用户登录会话
    /// </summary>
    /// <param name="user">登录的用户信息</param>
    /// <param name="token">认证令牌</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="user"/> 为 null 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="token"/> 为空或空白字符串时抛出</exception>
    /// <exception cref="InvalidOperationException">当会话操作失败时抛出</exception>
    void SetUserSession(UserDto user, string token);

    /// <summary>
    /// 清除用户登录会话
    /// </summary>
    /// <exception cref="InvalidOperationException">当会话操作失败时抛出</exception>
    void ClearUserSession();

    /// <summary>
    /// 重置所有会话状态到初始状态
    /// </summary>
    /// <exception cref="InvalidOperationException">当重置操作失败时抛出</exception>
    void Reset();

    /// <summary>
    /// 获取一个值，指示是否有活跃的诊疗会话
    /// </summary>
    /// <value>如果有活跃会话则为 true；否则为 false</value>
    bool HasActiveSession { get; }

    /// <summary>
    /// 获取一个值，指示用户是否已登录
    /// </summary>
    /// <value>如果用户已登录且有有效令牌则为 true；否则为 false</value>
    bool IsLoggedIn { get; }

    /// <summary>
    /// 获取当前的认证令牌（只读）
    /// </summary>
    /// <value>JWT认证令牌，如果未登录则为 null</value>
    string? AuthToken { get; }

    #endregion
}

#region 事件参数类

/// <summary>
/// 患者变化事件参数
/// 包含旧患者信息、新患者信息和变化时间
/// </summary>
public class PatientChangedEventArgs : EventArgs
{
    /// <summary>
    /// 获取或设置原患者信息
    /// </summary>
    public PatientDto? OldPatient { get; set; }

    /// <summary>
    /// 获取或设置新患者信息
    /// </summary>
    public PatientDto? NewPatient { get; set; }

    /// <summary>
    /// 获取或设置变化发生的时间
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 诊疗变化事件参数
/// 包含诊疗信息和状态的变化详情
/// </summary>
public class ConsultationChangedEventArgs : EventArgs
{
    /// <summary>
    /// 获取或设置原诊疗信息
    /// </summary>
    public ConsultationDto? OldConsultation { get; set; }

    /// <summary>
    /// 获取或设置新诊疗信息
    /// </summary>
    public ConsultationDto? NewConsultation { get; set; }

    /// <summary>
    /// 获取或设置原诊疗状态
    /// </summary>
    public ConsultationStatus OldStatus { get; set; }

    /// <summary>
    /// 获取或设置新诊疗状态
    /// </summary>
    public ConsultationStatus NewStatus { get; set; }

    /// <summary>
    /// 获取或设置变化发生的时间
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 用户变化事件参数
/// 包含用户登录状态变化的详细信息
/// </summary>
public class UserChangedEventArgs : EventArgs
{
    /// <summary>
    /// 获取或设置原用户信息
    /// </summary>
    public UserDto? OldUser { get; set; }

    /// <summary>
    /// 获取或设置新用户信息
    /// </summary>
    public UserDto? NewUser { get; set; }

    /// <summary>
    /// 获取或设置是否为登录操作
    /// </summary>
    public bool IsLogin { get; set; }

    /// <summary>
    /// 获取或设置变化发生的时间
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 状态消息事件参数
/// 用于传递全局状态消息和类型信息
/// </summary>
public class StatusMessageEventArgs : EventArgs
{
    /// <summary>
    /// 获取或设置状态消息内容
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置消息类型
    /// </summary>
    public StatusMessageType MessageType { get; set; }

    /// <summary>
    /// 获取或设置消息生成的时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// 诊疗状态枚举
/// 定义诊疗会话的各个阶段和状态
/// </summary>
public enum ConsultationStatus
{
    /// <summary>未开始 - 诊疗会话尚未开始</summary>
    NotStarted = 0,

    /// <summary>进行中 - 诊疗会话正在进行</summary>
    InProgress = 1,

    /// <summary>诊断中 - 正在进行病情诊断</summary>
    Diagnosing = 2,

    /// <summary>开方中 - 正在开具处方</summary>
    Prescribing = 3,

    /// <summary>已完成 - 诊疗会话已正常完成</summary>
    Completed = 4,

    /// <summary>已暂停 - 诊疗会话暂时暂停</summary>
    Paused = 5,

    /// <summary>已取消 - 诊疗会话已被取消</summary>
    Cancelled = 6
}

/// <summary>
/// 状态消息类型枚举
/// 用于区分不同类型的状态消息
/// </summary>
public enum StatusMessageType
{
    /// <summary>信息 - 一般信息提示</summary>
    Info = 0,

    /// <summary>成功 - 操作成功的反馈</summary>
    Success = 1,

    /// <summary>警告 - 需要注意的警告信息</summary>
    Warning = 2,

    /// <summary>错误 - 错误和异常信息</summary>
    Error = 3
}

#endregion
