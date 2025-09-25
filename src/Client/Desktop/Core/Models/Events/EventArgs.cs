using Prism.Events;
using LYBT.Shared.Models.Contracts.Common;
using SharedCommon = LYBT.Shared.Models.Contracts.Common.SharedCommon;
using ErrorSeverity = LYBT.Shared.Models.Contracts.Common.SharedCommon.ErrorSeverity;

namespace LYBT.Desktop.Core.Models.Events
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
        /// 诊疗数据
        /// </summary>
        Consultations,

        /// <summary>
        /// 所有数据
        /// </summary>
        All
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
    // ErrorSeverity enum已移至LYBT.Shared.Models.Contracts.Common.SharedCommon

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
    /// 患者选中事件参数
    /// </summary>
    public class PatientSelectedEventArgs : EventArgs
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
    /// 诊疗开始事件参数
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
}