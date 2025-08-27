using System;
using System.ComponentModel;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services
{
    /// <summary>
    /// UltraThink 简化会话管理服务实现 - 替代Redux的轻量级状态管理
    /// </summary>
    public class SessionManager : Prism.Mvvm.BindableBase, ISessionManager
    {
        #region 私有字段
        
        private readonly ILogger<SessionManager> _logger;
        
        private PatientDto? _currentPatient;
        private ConsultationDto? _activeConsultation;
        private UserDto? _currentUser;
        private Guid? _currentMedicalCaseId;
        private ConsultationStatus _consultationStatus = ConsultationStatus.NotStarted;
        private string? _authToken;
        
        #endregion
        
        #region 构造函数
        
        public SessionManager(ILogger<SessionManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _logger.LogInformation("SessionManager 初始化完成");
        }
        
        #endregion
        
        #region 公共属性
        
        /// <summary>
        /// 当前选中患者
        /// </summary>
        public PatientDto? CurrentPatient
        {
            get => _currentPatient;
            set
            {
                if (SetProperty(ref _currentPatient, value))
                {
                    OnPatientChanged(value);
                    _logger.LogInformation($"当前患者已更改: {value?.Name ?? "null"}");
                }
            }
        }
        
        /// <summary>
        /// 当前活跃诊疗
        /// </summary>
        public ConsultationDto? ActiveConsultation
        {
            get => _activeConsultation;
            set
            {
                if (SetProperty(ref _activeConsultation, value))
                {
                    OnConsultationChanged(value, _consultationStatus);
                    _logger.LogInformation($"当前诊疗已更改: {value?.Id ?? Guid.Empty}");
                }
            }
        }
        
        /// <summary>
        /// 当前登录用户
        /// </summary>
        public UserDto? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (SetProperty(ref _currentUser, value))
                {
                    OnUserChanged(value, value != null);
                    _logger.LogInformation($"当前用户已更改: {value?.Username ?? "null"}");
                }
            }
        }
        
        /// <summary>
        /// 当前医疗案例ID
        /// </summary>
        public Guid? CurrentMedicalCaseId
        {
            get => _currentMedicalCaseId;
            set => SetProperty(ref _currentMedicalCaseId, value);
        }
        
        /// <summary>
        /// 诊疗状态
        /// </summary>
        public ConsultationStatus ConsultationStatus
        {
            get => _consultationStatus;
            set
            {
                if (SetProperty(ref _consultationStatus, value))
                {
                    OnConsultationChanged(_activeConsultation, value);
                    _logger.LogInformation($"诊疗状态已更改: {value}");
                }
            }
        }
        
        /// <summary>
        /// 检查是否有活跃会话
        /// </summary>
        public bool HasActiveSession => _currentPatient != null && _consultationStatus != ConsultationStatus.NotStarted;
        
        /// <summary>
        /// 检查是否已登录
        /// </summary>
        public bool IsLoggedIn => _currentUser != null && !string.IsNullOrEmpty(_authToken);
        
        #endregion
        
        #region 事件
        
        /// <summary>
        /// 患者选择变化事件
        /// </summary>
        public event EventHandler<PatientChangedEventArgs>? PatientChanged;
        
        /// <summary>
        /// 诊疗状态变化事件
        /// </summary>
        public event EventHandler<ConsultationChangedEventArgs>? ConsultationChanged;
        
        /// <summary>
        /// 用户状态变化事件
        /// </summary>
        public event EventHandler<UserChangedEventArgs>? UserChanged;
        
        /// <summary>
        /// 全局状态消息事件
        /// </summary>
        public event EventHandler<StatusMessageEventArgs>? StatusMessage;
        
        #endregion
        
        #region 会话管理方法
        
        /// <summary>
        /// 开始诊疗会话
        /// </summary>
        /// <param name="patient">患者信息</param>
        /// <param name="medicalCaseId">医疗案例ID（可选）</param>
        public void StartConsultation(PatientDto patient, Guid? medicalCaseId = null)
        {
            try
            {
                if (patient == null)
                {
                    PublishStatusMessage("无法开始诊疗：患者信息为空", StatusMessageType.Error);
                    return;
                }
                
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
                
                // 简化后的ConsultationDto创建
                ActiveConsultation = new ConsultationDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient.Id,
                    MedicalCaseId = CurrentMedicalCaseId.Value,
                    UserId = Guid.Empty, // 将在实际调用时设置
                    ConsultationTime = DateTime.Now,
                    Status = Shared.Models.Enums.CommonStatus.Enabled,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };
                
                PublishStatusMessage($"已开始诊疗会话：{patient.Name}", StatusMessageType.Success);
                _logger.LogInformation($"诊疗会话已开始 - 患者: {patient.Name}, 案例ID: {CurrentMedicalCaseId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始诊疗会话时发生异常");
                PublishStatusMessage("开始诊疗会话失败", StatusMessageType.Error);
            }
        }
        
        /// <summary>
        /// 结束当前诊疗会话
        /// </summary>
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
                
                // 清除诊疗状态
                ConsultationStatus = ConsultationStatus.Completed;
                ActiveConsultation = null;
                CurrentPatient = null;
                CurrentMedicalCaseId = null;
                ConsultationStatus = ConsultationStatus.NotStarted;
                
                PublishStatusMessage($"已结束诊疗会话：{patientName}", StatusMessageType.Info);
                _logger.LogInformation($"诊疗会话已结束 - 患者: {patientName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "结束诊疗会话时发生异常");
                PublishStatusMessage("结束诊疗会话失败", StatusMessageType.Error);
            }
        }
        
        /// <summary>
        /// 设置用户会话
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <param name="token">认证令牌</param>
        public void SetUserSession(UserDto user, string token)
        {
            try
            {
                if (user == null || string.IsNullOrEmpty(token))
                {
                    PublishStatusMessage("无法设置用户会话：用户信息或令牌为空", StatusMessageType.Error);
                    return;
                }
                
                CurrentUser = user;
                _authToken = token;
                
                PublishStatusMessage($"用户 {user.Username} 已登录", StatusMessageType.Success);
                _logger.LogInformation($"用户会话已设置 - 用户: {user.Username}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置用户会话时发生异常");
                PublishStatusMessage("用户会话设置失败", StatusMessageType.Error);
            }
        }
        
        /// <summary>
        /// 清除用户会话
        /// </summary>
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
                _logger.LogInformation($"用户会话已清除 - 用户: {userName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除用户会话时发生异常");
                PublishStatusMessage("用户会话清除失败", StatusMessageType.Error);
            }
        }
        
        /// <summary>
        /// 重置所有会话状态
        /// </summary>
        public void Reset()
        {
            try
            {
                _logger.LogInformation("重置所有会话状态");
                
                // 清除所有状态
                CurrentPatient = null;
                ActiveConsultation = null;
                CurrentUser = null;
                CurrentMedicalCaseId = null;
                ConsultationStatus = ConsultationStatus.NotStarted;
                _authToken = null;
                
                PublishStatusMessage("会话状态已重置", StatusMessageType.Info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置会话状态时发生异常");
                PublishStatusMessage("会话状态重置失败", StatusMessageType.Error);
            }
        }
        
        #endregion
        
        #region 私有事件处理方法
        
        /// <summary>
        /// 触发患者变化事件
        /// </summary>
        private void OnPatientChanged(PatientDto? newPatient)
        {
            try
            {
                var args = new PatientChangedEventArgs
                {
                    NewPatient = newPatient,
                    OldPatient = null // 可以保存旧值，这里简化处理
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
        private void OnConsultationChanged(ConsultationDto? newConsultation, ConsultationStatus newStatus)
        {
            try
            {
                var args = new ConsultationChangedEventArgs
                {
                    NewConsultation = newConsultation,
                    NewStatus = newStatus,
                    OldConsultation = null, // 可以保存旧值，这里简化处理
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
        private void OnUserChanged(UserDto? newUser, bool isLogin)
        {
            try
            {
                var args = new UserChangedEventArgs
                {
                    NewUser = newUser,
                    IsLogin = isLogin,
                    OldUser = null // 可以保存旧值，这里简化处理
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
                _logger.LogError(ex, "发布状态消息时发生异常");
            }
        }
        
        #endregion
    }
}