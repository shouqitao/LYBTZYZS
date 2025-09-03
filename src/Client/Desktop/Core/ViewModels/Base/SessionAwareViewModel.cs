using System;
using System.ComponentModel;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.ViewModels.Base
{
    /// <summary>
    /// UltraThink 会话感知ViewModel基类 - 替代Redux StateViewModel
    /// 自动集成SessionManager和NotificationService
    /// </summary>
    public abstract class SessionAwareViewModel : Prism.Mvvm.BindableBase, IDisposable
    {
        #region 受保护的服务字段
        
        protected readonly ISessionManager SessionManager;
        protected readonly INotificationService NotificationService;
        protected readonly ILogger Logger;
        
        private bool _disposed;
        
        #endregion
        
        #region 构造函数
        
        protected SessionAwareViewModel(
            ISessionManager sessionManager, 
            INotificationService notificationService,
            ILogger logger)
        {
            SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            NotificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // 订阅会话变化事件
            SubscribeToSessionEvents();
            
            Logger.LogDebug($"{GetType().Name} ViewModel 已初始化");
        }
        
        #endregion
        
        #region 受保护的会话属性
        
        /// <summary>
        /// 当前患者（只读）
        /// </summary>
        protected PatientDto? CurrentPatient => SessionManager.CurrentPatient;
        
        /// <summary>
        /// 当前诊疗（只读）
        /// </summary>
        protected ConsultationDto? ActiveConsultation => SessionManager.ActiveConsultation;
        
        /// <summary>
        /// 当前用户（只读）
        /// </summary>
        protected UserDto? CurrentUser => SessionManager.CurrentUser;
        
        /// <summary>
        /// 诊疗状态（只读）
        /// </summary>
        protected ConsultationStatus ConsultationStatus => SessionManager.ConsultationStatus;
        
        /// <summary>
        /// 是否有活跃会话
        /// </summary>
        protected bool HasActiveSession => SessionManager.HasActiveSession;
        
        /// <summary>
        /// 是否已登录
        /// </summary>
        protected bool IsLoggedIn => SessionManager.IsLoggedIn;
        
        #endregion
        
        #region 受保护的快捷方法
        
        /// <summary>
        /// 显示成功消息
        /// </summary>
        protected void ShowSuccess(string message, string? title = null)
        {
            NotificationService.ShowSuccess(message, title);
        }
        
        /// <summary>
        /// 显示错误消息
        /// </summary>
        protected void ShowError(string message, string? title = null)
        {
            NotificationService.ShowError(message, title);
        }
        
        /// <summary>
        /// 显示警告消息
        /// </summary>
        protected void ShowWarning(string message, string? title = null)
        {
            NotificationService.ShowWarning(message, title);
        }
        
        /// <summary>
        /// 显示信息消息
        /// </summary>
        protected void ShowInfo(string message, string? title = null)
        {
            NotificationService.ShowInfo(message, title);
        }
        
        /// <summary>
        /// 显示加载状态
        /// </summary>
        protected void ShowLoading(string message = "正在加载...")
        {
            NotificationService.ShowLoading(message);
        }
        
        /// <summary>
        /// 隐藏加载状态
        /// </summary>
        protected void HideLoading()
        {
            NotificationService.HideLoading();
        }
        
        /// <summary>
        /// 记录信息日志
        /// </summary>
        protected void LogInfo(string message, params object[] args)
        {
            Logger.LogInformation(message, args);
        }
        
        /// <summary>
        /// 记录警告日志
        /// </summary>
        protected void LogWarning(string message, params object[] args)
        {
            Logger.LogWarning(message, args);
        }
        
        /// <summary>
        /// 记录错误日志
        /// </summary>
        protected void LogError(Exception exception, string message, params object[] args)
        {
            Logger.LogError(exception, message, args);
        }
        
        #endregion
        
        #region 虚拟事件处理方法
        
        /// <summary>
        /// 当患者变化时调用（子类可重写）
        /// </summary>
        /// <param name="args">患者变化事件参数</param>
        protected virtual void OnPatientChanged(PatientChangedEventArgs args)
        {
            LogInfo($"患者已变更: {args.NewPatient?.Name ?? "null"}");
            
            // 通知所有属性变更
            RaisePropertyChanged(nameof(CurrentPatient));
            RaisePropertyChanged(nameof(HasActiveSession));
        }
        
        /// <summary>
        /// 当诊疗状态变化时调用（子类可重写）
        /// </summary>
        /// <param name="args">诊疗变化事件参数</param>
        protected virtual void OnConsultationChanged(ConsultationChangedEventArgs args)
        {
            LogInfo($"诊疗状态已变更: {args.NewStatus}");
            
            // 通知所有属性变更
            RaisePropertyChanged(nameof(ActiveConsultation));
            RaisePropertyChanged(nameof(ConsultationStatus));
            RaisePropertyChanged(nameof(HasActiveSession));
        }
        
        /// <summary>
        /// 当用户状态变化时调用（子类可重写）
        /// </summary>
        /// <param name="args">用户变化事件参数</param>
        protected virtual void OnUserChanged(UserChangedEventArgs args)
        {
            LogInfo($"用户状态已变更: {args.NewUser?.Username ?? "null"}, 登录状态: {args.IsLogin}");
            
            // 通知所有属性变更
            RaisePropertyChanged(nameof(CurrentUser));
            RaisePropertyChanged(nameof(IsLoggedIn));
        }
        
        /// <summary>
        /// 当收到状态消息时调用（子类可重写）
        /// </summary>
        /// <param name="args">状态消息事件参数</param>
        protected virtual void OnStatusMessage(StatusMessageEventArgs args)
        {
            LogInfo($"状态消息: {args.MessageType} - {args.Message}");
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 订阅会话事件
        /// </summary>
        private void SubscribeToSessionEvents()
        {
            try
            {
                SessionManager.PatientChanged += SessionManager_PatientChanged;
                SessionManager.ConsultationChanged += SessionManager_ConsultationChanged;
                SessionManager.UserChanged += SessionManager_UserChanged;
                SessionManager.StatusMessage += SessionManager_StatusMessage;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "订阅会话事件时发生异常");
            }
        }
        
        /// <summary>
        /// 取消订阅会话事件
        /// </summary>
        private void UnsubscribeFromSessionEvents()
        {
            try
            {
                if (SessionManager != null)
                {
                    SessionManager.PatientChanged -= SessionManager_PatientChanged;
                    SessionManager.ConsultationChanged -= SessionManager_ConsultationChanged;
                    SessionManager.UserChanged -= SessionManager_UserChanged;
                    SessionManager.StatusMessage -= SessionManager_StatusMessage;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "取消订阅会话事件时发生异常");
            }
        }
        
        #endregion
        
        #region 事件处理器
        
        private void SessionManager_PatientChanged(object? sender, PatientChangedEventArgs e)
        {
            try
            {
                OnPatientChanged(e);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "处理患者变化事件时发生异常");
            }
        }
        
        private void SessionManager_ConsultationChanged(object? sender, ConsultationChangedEventArgs e)
        {
            try
            {
                OnConsultationChanged(e);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "处理诊疗变化事件时发生异常");
            }
        }
        
        private void SessionManager_UserChanged(object? sender, UserChangedEventArgs e)
        {
            try
            {
                OnUserChanged(e);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "处理用户变化事件时发生异常");
            }
        }
        
        private void SessionManager_StatusMessage(object? sender, StatusMessageEventArgs e)
        {
            try
            {
                OnStatusMessage(e);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "处理状态消息事件时发生异常");
            }
        }
        
        #endregion
        
        #region IDisposable 实现
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// 释放资源的实际实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 取消订阅事件
                    UnsubscribeFromSessionEvents();
                    
                    Logger.LogDebug($"{GetType().Name} ViewModel 已释放");
                }
                
                _disposed = true;
            }
        }
        
        /// <summary>
        /// 析构函数
        /// </summary>
        ~SessionAwareViewModel()
        {
            Dispose(false);
        }
        
        #endregion
    }
}