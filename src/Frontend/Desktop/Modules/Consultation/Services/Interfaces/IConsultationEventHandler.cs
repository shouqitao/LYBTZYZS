using System;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.Prescriptions;
using Prism.Events;

namespace LYBT.WPF.Client.Modules.Consultation.Services.Interfaces
{
    /// <summary>
    /// 看诊事件处理器接口
    /// </summary>
    public interface IConsultationEventHandler
    {
        /// <summary>
        /// 发布患者选择事件
        /// </summary>
        void PublishPatientSelected(PatientInfo patient);

        /// <summary>
        /// 订阅患者选择事件
        /// </summary>
        void SubscribeToPatientSelection(Action<PatientInfo> handler);

        /// <summary>
        /// 发布看诊开始事件
        /// </summary>
        void PublishConsultationStarted(ConsultationInfo consultation);

        /// <summary>
        /// 订阅看诊开始事件
        /// </summary>
        void SubscribeToConsultationStart(Action<ConsultationInfo> handler);

        /// <summary>
        /// 发布看诊完成事件
        /// </summary>
        void PublishConsultationCompleted(ConsultationInfo consultation);

        /// <summary>
        /// 订阅看诊完成事件
        /// </summary>
        void SubscribeToConsultationCompletion(Action<ConsultationInfo> handler);

        /// <summary>
        /// 发布处方保存事件
        /// </summary>
        void PublishPrescriptionSaved(PrescriptionInfo prescription);

        /// <summary>
        /// 订阅处方保存事件
        /// </summary>
        void SubscribeToPrescriptionSave(Action<PrescriptionInfo> handler);

        /// <summary>
        /// 发布数据刷新请求事件
        /// </summary>
        void PublishDataRefreshRequest(DataRefreshType refreshType);

        /// <summary>
        /// 订阅数据刷新请求事件
        /// </summary>
        void SubscribeToDataRefreshRequest(Action<DataRefreshType> handler);

        /// <summary>
        /// 发布错误事件
        /// </summary>
        void PublishError(string module, string message, Exception? exception = null);

        /// <summary>
        /// 订阅错误事件
        /// </summary>
        void SubscribeToErrors(Action<ErrorEventArgs> handler);

        /// <summary>
        /// 发布导航请求事件
        /// </summary>
        void PublishNavigationRequest(string viewName, object? parameters = null);

        /// <summary>
        /// 订阅导航请求事件
        /// </summary>
        void SubscribeToNavigationRequest(Action<NavigationEventArgs> handler);

        /// <summary>
        /// 发布状态消息
        /// </summary>
        void PublishStatusMessage(string message, StatusMessageType type = StatusMessageType.Info);

        /// <summary>
        /// 订阅状态消息
        /// </summary>
        void SubscribeToStatusMessages(Action<StatusMessageEventArgs> handler);

        /// <summary>
        /// 清理所有订阅
        /// </summary>
        void UnsubscribeAll();
    }

    #region 事件参数类

    /// <summary>
    /// 数据刷新类型
    /// </summary>
    public enum DataRefreshType
    {
        /// <summary>
        /// 刷新全部
        /// </summary>
        All,

        /// <summary>
        /// 刷新患者列表
        /// </summary>
        Patients,

        /// <summary>
        /// 刷新药材列表
        /// </summary>
        Herbs,

        /// <summary>
        /// 刷新验方列表
        /// </summary>
        Formulas,

        /// <summary>
        /// 刷新看诊记录
        /// </summary>
        Consultations,

        /// <summary>
        /// 刷新处方列表
        /// </summary>
        Prescriptions
    }

    /// <summary>
    /// 错误事件参数
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public string Module { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
    }

    /// <summary>
    /// 错误严重程度
    /// </summary>
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// 导航事件参数
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        public string ViewName { get; set; } = string.Empty;
        public object? Parameters { get; set; }
        public bool IsModal { get; set; }
    }

    /// <summary>
    /// 状态消息事件参数
    /// </summary>
    public class StatusMessageEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
        public StatusMessageType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int? DisplayDuration { get; set; } // 毫秒
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

    #region Prism事件定义

    /// <summary>
    /// 患者选择事件
    /// </summary>
    public class PatientSelectedEvent : PubSubEvent<PatientInfo> { }

    /// <summary>
    /// 看诊开始事件
    /// </summary>
    public class ConsultationStartedEvent : PubSubEvent<ConsultationInfo> { }

    /// <summary>
    /// 看诊完成事件
    /// </summary>
    public class ConsultationCompletedEvent : PubSubEvent<ConsultationInfo> { }

    /// <summary>
    /// 处方保存事件
    /// </summary>
    public class PrescriptionSavedEvent : PubSubEvent<PrescriptionInfo> { }

    /// <summary>
    /// 数据刷新请求事件
    /// </summary>
    public class DataRefreshRequestEvent : PubSubEvent<DataRefreshType> { }

    /// <summary>
    /// 错误事件
    /// </summary>
    public class ErrorOccurredEvent : PubSubEvent<ErrorEventArgs> { }

    /// <summary>
    /// 导航请求事件
    /// </summary>
    public class NavigationRequestEvent : PubSubEvent<NavigationEventArgs> { }

    /// <summary>
    /// 状态消息事件
    /// </summary>
    public class StatusMessageEvent : PubSubEvent<StatusMessageEventArgs> { }

    #endregion
}