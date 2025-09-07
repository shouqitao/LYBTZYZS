using Prism.Events;

namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// UltraThink重构: 统一事件架构设计文档
    ///
    /// 设计原则:
    /// 1. 所有事件使用 PubSubEvent<TEventData> 模式
    /// 2. 事件数据模型独立定义，支持序列化
    /// 3. 业务相关枚举值，便于理解和维护
    /// 4. 统一的错误处理和状态管理
    /// </summary>

    #region 统一事件数据模型

    /// <summary>
    /// 事件数据基类 - 提供通用元数据
    /// </summary>
    public abstract class EventDataBase
    {

        /// <summary>事件唯一标识</summary>
        public Guid EventId { get; set; } = Guid.NewGuid();

        /// <summary>事件时间戳</summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>事件来源模块</summary>
        public string SourceModule { get; set; } = string.Empty;

        /// <summary>相关消息</summary>
        public string? Message { get; set; }

        /// <summary>事件上下文数据</summary>
        public object? Context { get; set; }
    }

    /// <summary>
    /// 患者选择事件数据
    /// </summary>
    public class PatientSelectedData : EventDataBase
    {
        public Guid PatientId { get; set; } = Guid.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string? PatientIdNumber { get; set; }
        public int PatientAge { get; set; } = 0;
        public string? Gender { get; set; }
    }

    /// <summary>
    /// 诊疗开始事件数据
    /// </summary>
    public class ConsultationStartedData : EventDataBase
    {
        public Guid ConsultationId { get; set; } = Guid.Empty;
        public Guid PatientId { get; set; } = Guid.Empty;
        public string PatientName { get; set; } = string.Empty;
        public Guid DoctorId { get; set; } = Guid.Empty;
        public string? DoctorName { get; set; }
        public Guid MedicalCaseId { get; set; } = Guid.Empty;
    }

    /// <summary>
    /// 新的诊疗完成事件数据（避免与ConsultationEvents.cs中的重复）
    /// </summary>
    public class ConsultationCompletedDataNew : EventDataBase
    {
        public Guid ConsultationId { get; set; } = Guid.Empty;
        public Guid PatientId { get; set; } = Guid.Empty;
        public string PatientName { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; } = false;
        public string? ErrorMessage { get; set; }
        public Guid? PrescriptionId { get; set; }
        public decimal? TotalAmount { get; set; }
    }

    /// <summary>
    /// 处方保存事件数据
    /// </summary>
    public class PrescriptionSavedData : EventDataBase
    {
        public Guid PrescriptionId { get; set; } = Guid.Empty;
        public Guid PatientId { get; set; } = Guid.Empty;
        public string PatientName { get; set; } = string.Empty;
        public Guid ConsultationId { get; set; } = Guid.Empty;
        public decimal TotalAmount { get; set; } = 0m;
        public int HerbCount { get; set; } = 0;
        public string? PrescriptionNumber { get; set; }
    }

    /// <summary>
    /// 数据刷新请求事件数据
    /// </summary>
    public class DataRefreshRequestData : EventDataBase
    {
        public DataRefreshScope RefreshScope { get; set; } = DataRefreshScope.All;
        public string? TargetModule { get; set; }
        public bool ForceRefresh { get; set; } = false;
    }

    /// <summary>
    /// 导航请求事件数据
    /// </summary>
    public class NavigationRequestData : EventDataBase
    {
        public string ViewName { get; set; } = string.Empty;
        public object? Parameters { get; set; }
        public string? RegionName { get; set; }
    }

    /// <summary>
    /// 状态消息事件数据
    /// </summary>
    public class StatusMessageData : EventDataBase
    {
        public StatusMessageType MessageType { get; set; } = 0;
        public int DisplayDuration { get; set; } = 3000; // 毫秒
        public bool IsAutoDismiss { get; set; } = true;
    }

    /// <summary>
    /// 错误事件数据
    /// </summary>
    public class ErrorEventData : EventDataBase
    {
        public string ErrorMessage { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
        public string? ErrorCode { get; set; }
        public object? ErrorContext { get; set; }
    }

    #endregion 统一事件数据模型

    #region 统一业务枚举

    /// <summary>
    /// 数据刷新范围 - 业务导向的枚举设计
    /// </summary>
    public enum DataRefreshScope
    {

        /// <summary>全量刷新所有数据</summary>
        All = 0,

        /// <summary>刷新患者相关数据</summary>
        Patients = 1,

        /// <summary>刷新诊疗相关数据</summary>
        Consultations = 2,

        /// <summary>刷新处方相关数据</summary>
        Prescriptions = 3,

        /// <summary>刷新中药材数据</summary>
        Herbs = 4,

        /// <summary>刷新验方模板数据</summary>
        Formulas = 5,

        /// <summary>刷新医疗案例数据</summary>
        MedicalCases = 6,

        /// <summary>刷新用户数据</summary>
        Users = 7
    }

    // StatusMessageType枚举已在ConsultationEventArgs.cs中定义，这里不重复定义

    /// <summary>
    /// 错误严重程度
    /// </summary>
    public enum ErrorSeverity
    {

        /// <summary>信息级别</summary>
        Info = 0,

        /// <summary>警告级别</summary>
        Warning = 1,

        /// <summary>错误级别</summary>
        Error = 2,

        /// <summary>严重错误</summary>
        Critical = 3,

        /// <summary>致命错误</summary>
        Fatal = 4
    }

    #endregion 统一业务枚举

    #region 新的统一事件定义（避免与现有事件冲突）

    /// <summary>
    /// 新的患者选择事件 - 使用新数据模型
    /// </summary>
    public class PatientSelectedEventNew : PubSubEvent<PatientSelectedData>
    {
    }

    /// <summary>
    /// 新的诊疗开始事件 - 使用新数据模型
    /// </summary>
    public class ConsultationStartedEventNew : PubSubEvent<ConsultationStartedData>
    {
    }

    /// <summary>
    /// 新的诊疗完成事件 - 使用新数据模型（避免与现有ConsultationCompletedEvent冲突）
    /// </summary>
    public class ConsultationCompletedEventNew : PubSubEvent<ConsultationCompletedDataNew>
    {
    }

    /// <summary>
    /// 处方保存事件
    /// </summary>
    public class PrescriptionSavedEvent : PubSubEvent<PrescriptionSavedData>
    {
    }

    /// <summary>
    /// 新的数据刷新请求事件 - 使用新数据模型
    /// </summary>
    public class DataRefreshRequestEventNew : PubSubEvent<DataRefreshRequestData>
    {
    }

    /// <summary>
    /// 新的导航请求事件 - 使用新数据模型
    /// </summary>
    public class NavigationRequestEventNew : PubSubEvent<NavigationRequestData>
    {
    }

    /// <summary>
    /// 新的状态消息事件 - 使用新数据模型
    /// </summary>
    public class StatusMessageEventNew : PubSubEvent<StatusMessageData>
    {
    }

    /// <summary>
    /// 新的错误发生事件 - 使用新数据模型
    /// </summary>
    public class ErrorOccurredEventNew : PubSubEvent<ErrorEventData>
    {
    }

    #endregion 新的统一事件定义（避免与现有事件冲突）

    #region 向后兼容适配器

    /// <summary>
    /// 向后兼容性适配器 - 将旧的EventArgs模式适配到新架构
    /// 在迁移期间保持API兼容性
    /// </summary>
    public static class EventCompatibilityAdapter
    {

        /// <summary>
        /// 将PatientInfo转换为PatientSelectedData
        /// </summary>
        public static PatientSelectedData FromPatientInfo(object patientInfo)
        {
            // 实现类型转换逻辑
            // 支持从PatientInfo/PatientDto/PatientSelectedEventArgs等多种类型转换
            return new PatientSelectedData
            {
                SourceModule = "Consultation",
                Message = "患者已选择"
                // 根据输入类型设置具体字段
            };
        }

        /// <summary>
        /// 创建标准的ConsultationStartedData
        /// </summary>
        public static ConsultationStartedData CreateConsultationStarted(
            Guid consultationId,
            Guid patientId,
            string patientName)
        {
            return new ConsultationStartedData
            {
                ConsultationId = consultationId,
                PatientId = patientId,
                PatientName = patientName,
                SourceModule = "Consultation",
                Message = "诊疗已开始"
            };
        }

        /// <summary>
        /// 创建标准的PrescriptionSavedData
        /// </summary>
        public static PrescriptionSavedData CreatePrescriptionSaved(
            Guid prescriptionId,
            Guid patientId,
            string patientName,
            decimal totalAmount)
        {
            return new PrescriptionSavedData
            {
                PrescriptionId = prescriptionId,
                PatientId = patientId,
                PatientName = patientName,
                TotalAmount = totalAmount,
                SourceModule = "Consultation",
                Message = "处方已保存"
            };
        }
    }

    #endregion 向后兼容适配器
}
