using System;
using Prism.Events;

namespace LYBT.WPF.Client.Core.Models.Events
{
    /// <summary>
    /// 数据刷新类型
    /// </summary>
    public enum DataRefreshType
    {
        /// <summary>
        /// 患者数据
        /// </summary>
        Patients,

        /// <summary>
        /// 中药材数据
        /// </summary>
        Herbs,

        /// <summary>
        /// 验方数据
        /// </summary>
        Formulas,

        /// <summary>
        /// 处方数据
        /// </summary>
        Prescriptions,

        /// <summary>
        /// 看诊数据
        /// </summary>
        Consultations,

        /// <summary>
        /// 所有数据
        /// </summary>
        All
    }

    /// <summary>
    /// 状态消息类型
    /// </summary>
    public enum StatusMessageType
    {
        /// <summary>
        /// 信息
        /// </summary>
        Info,

        /// <summary>
        /// 成功
        /// </summary>
        Success,

        /// <summary>
        /// 警告
        /// </summary>
        Warning,

        /// <summary>
        /// 错误
        /// </summary>
        Error
    }

    /// <summary>
    /// 验方合并模式
    /// </summary>
    public enum FormulaMergeMode
    {
        /// <summary>
        /// 替换现有处方
        /// </summary>
        Replace,

        /// <summary>
        /// 追加到现有处方
        /// </summary>
        Append,

        /// <summary>
        /// 与现有处方合并
        /// </summary>
        Merge
    }

    /// <summary>
    /// 错误严重程度
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>严重错误</summary>
        Critical = 4,
        /// <summary>错误</summary>
        Error = 3,
        /// <summary>警告</summary>
        Warning = 2,
        /// <summary>信息</summary>
        Info = 1
    }

    /// <summary>
    /// 错误事件参数
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public string? Context { get; set; }
        public string? Module { get; set; }
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public ErrorEventArgs(string message)
        {
            Message = message;
        }

        public ErrorEventArgs(string message, Exception exception) : this(message)
        {
            Exception = exception;
        }

        public ErrorEventArgs(string message, Exception exception, string context) : this(message, exception)
        {
            Context = context;
        }

        public ErrorEventArgs(string message, ErrorSeverity severity) : this(message)
        {
            Severity = severity;
        }

        public ErrorEventArgs(string message, string module, ErrorSeverity severity) : this(message, severity)
        {
            Module = module;
        }
    }

    /// <summary>
    /// 导航事件参数
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        public string Target { get; set; } = string.Empty;
        public string ViewName { get; set; } = string.Empty;
        public bool IsModal { get; set; }
        public object? Parameters { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public NavigationEventArgs(string target)
        {
            Target = target;
            ViewName = target;
        }

        public NavigationEventArgs(string target, object parameters) : this(target)
        {
            Parameters = parameters;
        }

        public NavigationEventArgs(string target, bool isModal) : this(target)
        {
            IsModal = isModal;
        }

        public NavigationEventArgs(string target, object parameters, bool isModal) : this(target, parameters)
        {
            IsModal = isModal;
        }
    }

    /// <summary>
    /// 状态消息事件参数
    /// </summary>
    public class StatusMessageEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
        public StatusMessageType Type { get; set; } = StatusMessageType.Info;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int? Duration { get; set; }
        public int? DisplayDuration { get; set; }

        public StatusMessageEventArgs(string message, StatusMessageType type = StatusMessageType.Info)
        {
            Message = message;
            Type = type;
        }

        public StatusMessageEventArgs(string message, StatusMessageType type, int duration) : this(message, type)
        {
            Duration = duration;
            DisplayDuration = duration;
        }
    }

    /// <summary>
    /// 患者选择事件
    /// </summary>
    public class PatientSelectedEvent : PubSubEvent<PatientSelectedEventArgs>
    {
    }

    /// <summary>
    /// 患者选择事件参数
    /// </summary>
    public class PatientSelectedEventArgs
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public PatientSelectedEventArgs(Guid patientId, string patientName)
        {
            PatientId = patientId;
            PatientName = patientName;
        }
    }

    /// <summary>
    /// 看诊开始事件
    /// </summary>
    public class ConsultationStartedEvent : PubSubEvent<ConsultationStartedEventArgs>
    {
    }

    /// <summary>
    /// 看诊开始事件参数
    /// </summary>
    public class ConsultationStartedEventArgs
    {
        public Guid ConsultationId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public ConsultationStartedEventArgs(Guid consultationId, Guid patientId, string patientName)
        {
            ConsultationId = consultationId;
            PatientId = patientId;
            PatientName = patientName;
            StartTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 看诊完成事件
    /// </summary>
    public class ConsultationCompletedEvent : PubSubEvent<ConsultationCompletedEventArgs>
    {
    }

    /// <summary>
    /// 看诊完成事件参数
    /// </summary>
    public class ConsultationCompletedEventArgs
    {
        public Guid ConsultationId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime EndTime { get; set; }
        public string? Summary { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public ConsultationCompletedEventArgs(Guid consultationId, Guid patientId, string patientName)
        {
            ConsultationId = consultationId;
            PatientId = patientId;
            PatientName = patientName;
            EndTime = DateTime.Now;
        }

        public ConsultationCompletedEventArgs(Guid consultationId, Guid patientId, string patientName, string summary) 
            : this(consultationId, patientId, patientName)
        {
            Summary = summary;
        }
    }

    /// <summary>
    /// 错误发生事件
    /// </summary>
    public class ErrorOccurredEvent : PubSubEvent<ErrorEventArgs>
    {
    }

    /// <summary>
    /// 导航请求事件
    /// </summary>
    public class NavigationRequestEvent : PubSubEvent<NavigationEventArgs>
    {
    }

    /// <summary>
    /// 状态消息事件
    /// </summary>
    public class StatusMessageEvent : PubSubEvent<StatusMessageEventArgs>
    {
    }

    /// <summary>
    /// 数据刷新请求事件
    /// </summary>
    public class DataRefreshRequestEvent : PubSubEvent<DataRefreshRequestEventArgs>
    {
    }

    /// <summary>
    /// 数据刷新请求事件参数
    /// </summary>
    public class DataRefreshRequestEventArgs
    {
        public DataRefreshType RefreshType { get; set; }
        public string? FilterCriteria { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public DataRefreshRequestEventArgs(DataRefreshType refreshType)
        {
            RefreshType = refreshType;
        }

        public DataRefreshRequestEventArgs(DataRefreshType refreshType, string filterCriteria) : this(refreshType)
        {
            FilterCriteria = filterCriteria;
        }
    }

    /// <summary>
    /// 处方保存事件
    /// </summary>
    public class PrescriptionSavedEvent : PubSubEvent<PrescriptionSavedEventArgs>
    {
    }

    /// <summary>
    /// 处方保存事件参数
    /// </summary>
    public class PrescriptionSavedEventArgs
    {
        public Guid PrescriptionId { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public PrescriptionSavedEventArgs(Guid prescriptionId, Guid patientId, string patientName, decimal totalAmount)
        {
            PrescriptionId = prescriptionId;
            PatientId = patientId;
            PatientName = patientName;
            TotalAmount = totalAmount;
        }
    }
}