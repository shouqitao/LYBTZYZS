using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services;

/// <summary>
/// 会话管理器 - 负责管理用户登录状态、患者选择和诊疗会话
/// 采用UltraThink架构标准，使用C# 12主构造函数和现代化特性
/// 替代Redux的轻量级状态管理解决方案
/// </summary>
/// <param name="logger">日志记录器，用于记录会话状态变化和异常信息</param>
public class SessionManager(ILogger<SessionManager> logger) : Prism.Mvvm.BindableBase, ISessionManager
{

    #region 私有字段

    private readonly ILogger<SessionManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private PatientDto? _currentPatient;
    private ConsultationDto? _activeConsultation;
    private UserDto? _currentUser;
    private Guid? _currentMedicalCaseId;
    private ConsultationStatus _consultationStatus = ConsultationStatus.NotStarted;
    private string? _authToken;

    #endregion 私有字段

    #region 公共属性

    /// <summary>
    /// 获取或设置当前选中患者
    /// </summary>
    /// <value>当前患者的详细信息，如果未选择患者则为 null</value>
    public PatientDto? CurrentPatient
    {
        get => _currentPatient;
        set
        {
            if (SetProperty(ref _currentPatient, value))
            {
                OnPatientChanged(value);
                _logger.LogInformation("当前患者已更改: {PatientName}", value?.Name ?? "null");
            }
        }
    }

    /// <summary>
    /// 获取或设置当前活跃诊疗会话
    /// </summary>
    /// <value>当前诊疗会话信息，如果没有活跃诊疗则为 null</value>
    public ConsultationDto? ActiveConsultation
    {
        get => _activeConsultation;
        set
        {
            if (SetProperty(ref _activeConsultation, value))
            {
                OnConsultationChanged(value, _consultationStatus);
                _logger.LogInformation("当前诊疗已更改: {ConsultationId}", value?.Id ?? Guid.Empty);
            }
        }
    }

    /// <summary>
    /// 获取或设置当前登录用户
    /// </summary>
    /// <value>当前登录用户信息，如果未登录则为 null</value>
    public UserDto? CurrentUser
    {
        get => _currentUser;
        set
        {
            if (SetProperty(ref _currentUser, value))
            {
                OnUserChanged(value, value != null);
                _logger.LogInformation("当前用户已更改: {Username}", value?.Username ?? "null");
            }
        }
    }

    /// <summary>
    /// 获取或设置当前医疗案例ID
    /// </summary>
    /// <value>当前医疗案例的唯一标识符，如果没有活跃案例则为 null</value>
    public Guid? CurrentMedicalCaseId
    {
        get => _currentMedicalCaseId;
        set => SetProperty(ref _currentMedicalCaseId, value);
    }

    /// <summary>
    /// 获取或设置诊疗状态
    /// </summary>
    /// <value>当前诊疗的进行状态</value>
    public ConsultationStatus ConsultationStatus
    {
        get => _consultationStatus;
        set
        {
            if (SetProperty(ref _consultationStatus, value))
            {
                OnConsultationChanged(_activeConsultation, value);
                _logger.LogInformation("诊疗状态已更改: {Status}", value);
            }
        }
    }

    /// <summary>
    /// 获取一个值，指示是否有活跃的诊疗会话
    /// </summary>
    /// <value>如果有活跃会话则为 true；否则为 false</value>
    public bool HasActiveSession => _currentPatient != null && _consultationStatus != ConsultationStatus.NotStarted;

    /// <summary>
    /// 获取一个值，指示用户是否已登录
    /// </summary>
    /// <value>如果用户已登录且有有效令牌则为 true；否则为 false</value>
    public bool IsLoggedIn => _currentUser != null && !string.IsNullOrEmpty(_authToken);

    #endregion 公共属性

    #region 事件

    /// <summary>
    /// 当患者选择发生变化时引发此事件
    /// </summary>
    public event EventHandler<PatientChangedEventArgs>? PatientChanged;

    /// <summary>
    /// 当诊疗状态发生变化时引发此事件
    /// </summary>
    public event EventHandler<ConsultationChangedEventArgs>? ConsultationChanged;

    /// <summary>
    /// 当用户登录状态发生变化时引发此事件
    /// </summary>
    public event EventHandler<UserChangedEventArgs>? UserChanged;

    /// <summary>
    /// 当需要显示全局状态消息时引发此事件
    /// </summary>
    public event EventHandler<StatusMessageEventArgs>? StatusMessage;

    #endregion 事件

    #region 会话管理方法

    /// <summary>
    /// 开始新的诊疗会话
    /// </summary>
    /// <param name="patient">要开始诊疗的患者信息</param>
    /// <param name="medicalCaseId">可选的医疗案例ID，如果未提供将自动生成</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="patient"/> 为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">当会话操作失败时抛出</exception>
    public void StartConsultation(PatientDto patient, Guid? medicalCaseId = null)
    {
        ArgumentNullException.ThrowIfNull(patient, nameof(patient));

        try
        {
            // 如果有活跃会话，先结束当前会话
            if (HasActiveSession)
            {
                _logger.LogWarning("检测到活跃会话，先结束当前会话");
                EndConsultation();
            }

            // 设置新会话
            CurrentPatient = patient;
            CurrentMedicalCaseId = medicalCaseId ?? Guid.NewGuid();
            ConsultationStatus = ConsultationStatus.InProgress;

            // 创建新的诊疗记录
            ActiveConsultation = CreateNewConsultation(patient);

            PublishStatusMessage($"已开始诊疗会话：{patient.Name}", StatusMessageType.Success);
            _logger.LogInformation(
                "诊疗会话已开始 - 患者: {PatientName}, 案例ID: {CaseId}",
                patient.Name, CurrentMedicalCaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始诊疗会话时发生异常 - 患者: {PatientName}", patient.Name);
            PublishStatusMessage("开始诊疗会话失败", StatusMessageType.Error);
            throw new InvalidOperationException($"无法开始诊疗会话: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 结束当前的诊疗会话
    /// </summary>
    /// <exception cref="InvalidOperationException">当会话操作失败时抛出</exception>
    public void EndConsultation()
    {
        try
        {
            if (!HasActiveSession)
            {
                PublishStatusMessage("没有活跃的诊疗会话", StatusMessageType.Warning);
                return;
            }

            var patientName = CurrentPatient?.Name ?? "未知患者";

            // 清除诊疗状态 - 使用C# 12集合表达式
            ResetConsultationState();

            PublishStatusMessage($"已结束诊疗会话：{patientName}", StatusMessageType.Info);
            _logger.LogInformation("诊疗会话已结束 - 患者: {PatientName}", patientName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "结束诊疗会话时发生异常");
            PublishStatusMessage("结束诊疗会话失败", StatusMessageType.Error);
            throw new InvalidOperationException($"无法结束诊疗会话: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 设置用户登录会话
    /// </summary>
    /// <param name="user">登录的用户信息</param>
    /// <param name="token">认证令牌</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="user"/> 为 null 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="token"/> 为空或空白字符串时抛出</exception>
    /// <exception cref="InvalidOperationException">当会话操作失败时抛出</exception>
    public void SetUserSession(UserDto user, string token)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        ArgumentException.ThrowIfNullOrWhiteSpace(token, nameof(token));

        try
        {
            CurrentUser = user;
            _authToken = token;

            PublishStatusMessage($"用户 {user.Username} 已登录", StatusMessageType.Success);
            _logger.LogInformation(
                "用户会话已设置 - 用户: {Username}, 角色: {Role}",
                user.Username, user.Role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置用户会话时发生异常 - 用户: {Username}", user?.Username);
            PublishStatusMessage("用户会话设置失败", StatusMessageType.Error);
            throw new InvalidOperationException($"无法设置用户会话: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 清除用户登录会话
    /// </summary>
    /// <exception cref="InvalidOperationException">当会话操作失败时抛出</exception>
    public void ClearUserSession()
    {
        try
        {
            var userName = CurrentUser?.Username ?? "未知用户";

            // 如果有活跃诊疗，先结束
            if (HasActiveSession)
            {
                EndConsultation();
            }

            CurrentUser = null;
            _authToken = null;

            PublishStatusMessage($"用户 {userName} 已登出", StatusMessageType.Info);
            _logger.LogInformation("用户会话已清除 - 用户: {Username}", userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除用户会话时发生异常");
            PublishStatusMessage("用户会话清除失败", StatusMessageType.Error);
            throw new InvalidOperationException($"无法清除用户会话: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 重置所有会话状态到初始状态
    /// </summary>
    /// <exception cref="InvalidOperationException">当重置操作失败时抛出</exception>
    public void Reset()
    {
        try
        {
            _logger.LogInformation("重置所有会话状态");

            // 使用现代化的状态清理方式
            ResetAllStates();

            PublishStatusMessage("会话状态已重置", StatusMessageType.Info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置会话状态时发生异常");
            PublishStatusMessage("会话状态重置失败", StatusMessageType.Error);
            throw new InvalidOperationException($"无法重置会话状态: {ex.Message}", ex);
        }
    }

    #endregion 会话管理方法

    #region 私有辅助方法

    /// <summary>
    /// 创建新的诊疗记录
    /// </summary>
    /// <param name="patient">患者信息</param>
    /// <returns>新创建的诊疗记录</returns>
    private ConsultationDto CreateNewConsultation(PatientDto patient)
    {
        return new ConsultationDto
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            MedicalCaseId = CurrentMedicalCaseId!.Value,
            UserId = CurrentUser?.Id ?? Guid.Empty,
            ConsultationTime = DateTime.Now,
            Status = Shared.Models.Enums.CommonStatus.Enabled,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };
    }

    /// <summary>
    /// 重置诊疗相关状态
    /// </summary>
    private void ResetConsultationState()
    {
        ConsultationStatus = ConsultationStatus.Completed;
        ActiveConsultation = null;
        CurrentPatient = null;
        CurrentMedicalCaseId = null;
        ConsultationStatus = ConsultationStatus.NotStarted;
    }

    /// <summary>
    /// 重置所有状态到初始值
    /// </summary>
    private void ResetAllStates()
    {
        // 使用现代化的批量状态重置
        var resetActions = new Action[]
        {
            () => CurrentPatient = null,
            () => ActiveConsultation = null,
            () => CurrentUser = null,
            () => CurrentMedicalCaseId = null,
            () => ConsultationStatus = ConsultationStatus.NotStarted,
            () => _authToken = null
        };

        foreach (var resetAction in resetActions)
        {
            resetAction();
        }
    }

    #endregion 私有辅助方法

    #region 私有事件处理方法

    /// <summary>
    /// 触发患者变化事件
    /// </summary>
    /// <param name="newPatient">新患者信息</param>
    private void OnPatientChanged(PatientDto? newPatient)
    {
        try
        {
            var args = new PatientChangedEventArgs
            {
                NewPatient = newPatient,
                OldPatient = null // 简化处理，可在需要时保存旧值
            };

            PatientChanged?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发患者变化事件时发生异常");
        }
    }

    /// <summary>
    /// 触发诊疗变化事件
    /// </summary>
    /// <param name="newConsultation">新诊疗信息</param>
    /// <param name="newStatus">新诊疗状态</param>
    private void OnConsultationChanged(ConsultationDto? newConsultation, ConsultationStatus newStatus)
    {
        try
        {
            var args = new ConsultationChangedEventArgs
            {
                NewConsultation = newConsultation,
                NewStatus = newStatus,
                OldConsultation = null, // 简化处理，可在需要时保存旧值
                OldStatus = ConsultationStatus.NotStarted
            };

            ConsultationChanged?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发诊疗变化事件时发生异常");
        }
    }

    /// <summary>
    /// 触发用户变化事件
    /// </summary>
    /// <param name="newUser">新用户信息</param>
    /// <param name="isLogin">是否为登录操作</param>
    private void OnUserChanged(UserDto? newUser, bool isLogin)
    {
        try
        {
            var args = new UserChangedEventArgs
            {
                NewUser = newUser,
                IsLogin = isLogin,
                OldUser = null // 简化处理，可在需要时保存旧值
            };

            UserChanged?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发用户变化事件时发生异常");
        }
    }

    /// <summary>
    /// 发布状态消息
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <param name="type">消息类型</param>
    private void PublishStatusMessage(string message, StatusMessageType type)
    {
        try
        {
            var args = new StatusMessageEventArgs
            {
                Message = message,
                MessageType = type
            };

            StatusMessage?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布状态消息时发生异常 - 消息: {Message}", message);
        }
    }

    #endregion 私有事件处理方法
}
