using System;

namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 诊疗事件参数基类
    /// </summary>
    public abstract class ConsultationEventArgsBase : EventArgs
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? Message { get; set; }
    }

    /// <summary>
    /// 处方保存事件参数
    /// </summary>
    public class PrescriptionSavedEventArgs : ConsultationEventArgsBase
    {
        public Guid PrescriptionId { get; set; }
        public bool IsSuccess { get; set; }

        public PrescriptionSavedEventArgs() { }

        public PrescriptionSavedEventArgs(Guid prescriptionId, Guid patientId, string patientName, decimal totalAmount)
        {
            PrescriptionId = prescriptionId;
            Id = patientId;
            Message = $"处方已保存: {patientName}, 总金额: {totalAmount:C}";
            IsSuccess = true;
        }
    }

    /// <summary>
    /// 患者选择事件参数
    /// </summary>
    public class PatientSelectedEventArgs : ConsultationEventArgsBase
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;

        public PatientSelectedEventArgs() { }

        public PatientSelectedEventArgs(Guid patientId, string patientName)
        {
            PatientId = patientId;
            PatientName = patientName;
            Id = patientId;
            Message = $"已选择患者: {patientName}";
        }
    }

    /// <summary>
    /// 诊疗开始事件参数
    /// </summary>
    public class ConsultationStartedEventArgs : ConsultationEventArgsBase
    {
        public Guid ConsultationId { get; set; }
        public Guid PatientId { get; set; }

        public ConsultationStartedEventArgs() { }

        public ConsultationStartedEventArgs(Guid consultationId, Guid patientId, string? patientName)
        {
            ConsultationId = consultationId;
            PatientId = patientId;
            Id = consultationId;
            Message = $"诊疗已开始: {patientName ?? "患者"}";
        }
    }

    /// <summary>
    /// 诊疗完成事件参数
    /// </summary>
    public class ConsultationCompletedEventArgs : ConsultationEventArgsBase
    {
        public Guid ConsultationId { get; set; }
        public bool IsSuccess { get; set; }

        public ConsultationCompletedEventArgs() { }

        public ConsultationCompletedEventArgs(Guid consultationId, Guid patientId, string? patientName)
        {
            ConsultationId = consultationId;
            Id = consultationId;
            IsSuccess = true;
            Message = $"诊疗已完成: {patientName ?? "患者"}";
        }
    }

    /// <summary>
    /// 数据刷新请求事件参数
    /// </summary>
    public class DataRefreshRequestEventArgs : EventArgs
    {
        public DataRefreshType RefreshType { get; set; }
        public string TargetModule { get; set; } = string.Empty;

        public DataRefreshRequestEventArgs() { }

        public DataRefreshRequestEventArgs(DataRefreshType refreshType)
        {
            RefreshType = refreshType;
        }
    }

    /// <summary>
    /// 状态消息事件参数
    /// </summary>
    public class StatusMessageEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
        public StatusMessageType MessageType { get; set; }

        public StatusMessageEventArgs() { }

        public StatusMessageEventArgs(string message, StatusMessageType messageType, int duration = 3000)
        {
            Message = message;
            MessageType = messageType;
        }
    }

    /// <summary>
    /// 诊疗错误事件参数（避免与System.ComponentModel.ErrorEventArgs冲突）
    /// </summary>
    public class ConsultationErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public string Module { get; set; } = string.Empty;

        public ConsultationErrorEventArgs() { }

        public ConsultationErrorEventArgs(string message, Exception? exception)
        {
            ErrorMessage = message;
            Exception = exception;
        }

        public ConsultationErrorEventArgs(string message, string module, ErrorSeverity severity)
        {
            ErrorMessage = message;
            Module = module;
        }
    }
}

// 需要引用的枚举类型（需要先确保Enums目录存在）
namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 数据刷新类型
    /// </summary>
    public enum DataRefreshType
    {
        /// <summary>全量刷新</summary>
        Full = 0,
        /// <summary>部分刷新</summary>
        Partial = 1,
        /// <summary>增量刷新</summary>
        Incremental = 2,
        /// <summary>所有数据</summary>
        All = 3,
        /// <summary>看诊数据</summary>
        Consultations = 4,
        /// <summary>处方数据</summary>
        Prescriptions = 5
    }

    /// <summary>
    /// 状态消息类型
    /// </summary>
    public enum StatusMessageType
    {
        /// <summary>信息</summary>
        Info = 0,
        /// <summary>成功</summary>
        Success = 1,
        /// <summary>警告</summary>
        Warning = 2,
        /// <summary>错误</summary>
        Error = 3
    }
}