using System;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// UltraThink 简化会话管理服务 - 替代Redux的轻量级状态管理
    /// </summary>
    public interface ISessionManager
    {
        #region 当前会话状态
        
        /// <summary>
        /// 当前选中患者
        /// </summary>
        PatientDto? CurrentPatient { get; set; }
        
        /// <summary>
        /// 当前活跃诊疗
        /// </summary>
        ConsultationDto? ActiveConsultation { get; set; }
        
        /// <summary>
        /// 当前登录用户
        /// </summary>
        UserDto? CurrentUser { get; set; }
        
        /// <summary>
        /// 当前医疗案例ID
        /// </summary>
        Guid? CurrentMedicalCaseId { get; set; }
        
        /// <summary>
        /// 诊疗状态
        /// </summary>
        ConsultationStatus ConsultationStatus { get; set; }
        
        #endregion
        
        #region 状态变化事件
        
        /// <summary>
        /// 患者选择变化事件
        /// </summary>
        event EventHandler<PatientChangedEventArgs>? PatientChanged;
        
        /// <summary>
        /// 诊疗状态变化事件
        /// </summary>
        event EventHandler<ConsultationChangedEventArgs>? ConsultationChanged;
        
        /// <summary>
        /// 用户状态变化事件
        /// </summary>
        event EventHandler<UserChangedEventArgs>? UserChanged;
        
        /// <summary>
        /// 全局状态消息事件
        /// </summary>
        event EventHandler<StatusMessageEventArgs>? StatusMessage;
        
        #endregion
        
        #region 会话管理方法
        
        /// <summary>
        /// 开始诊疗会话
        /// </summary>
        /// <param name="patient">患者信息</param>
        /// <param name="medicalCaseId">医疗案例ID（可选）</param>
        void StartConsultation(PatientDto patient, Guid? medicalCaseId = null);
        
        /// <summary>
        /// 结束当前诊疗会话
        /// </summary>
        void EndConsultation();
        
        /// <summary>
        /// 设置用户会话
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <param name="token">认证令牌</param>
        void SetUserSession(UserDto user, string token);
        
        /// <summary>
        /// 清除用户会话
        /// </summary>
        void ClearUserSession();
        
        /// <summary>
        /// 重置所有会话状态
        /// </summary>
        void Reset();
        
        /// <summary>
        /// 检查是否有活跃会话
        /// </summary>
        bool HasActiveSession { get; }
        
        /// <summary>
        /// 检查是否已登录
        /// </summary>
        bool IsLoggedIn { get; }
        
        #endregion
    }
    
    #region 事件参数类
    
    /// <summary>
    /// 患者变化事件参数
    /// </summary>
    public class PatientChangedEventArgs : EventArgs
    {
        public PatientDto? OldPatient { get; set; }
        public PatientDto? NewPatient { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// 诊疗变化事件参数
    /// </summary>
    public class ConsultationChangedEventArgs : EventArgs
    {
        public ConsultationDto? OldConsultation { get; set; }
        public ConsultationDto? NewConsultation { get; set; }
        public ConsultationStatus OldStatus { get; set; }
        public ConsultationStatus NewStatus { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// 用户变化事件参数
    /// </summary>
    public class UserChangedEventArgs : EventArgs
    {
        public UserDto? OldUser { get; set; }
        public UserDto? NewUser { get; set; }
        public bool IsLogin { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// 状态消息事件参数
    /// </summary>
    public class StatusMessageEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
        public StatusMessageType MessageType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    /// <summary>
    /// 诊疗状态枚举
    /// </summary>
    public enum ConsultationStatus
    {
        /// <summary>未开始</summary>
        NotStarted,
        /// <summary>进行中</summary>
        InProgress,
        /// <summary>诊断中</summary>
        Diagnosing,
        /// <summary>开方中</summary>
        Prescribing,
        /// <summary>已完成</summary>
        Completed,
        /// <summary>已暂停</summary>
        Paused,
        /// <summary>已取消</summary>
        Cancelled
    }
    
    /// <summary>
    /// 状态消息类型
    /// </summary>
    public enum StatusMessageType
    {
        Info,
        Success,
        Warning,
        Error
    }
    
    #endregion
}