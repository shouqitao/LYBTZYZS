using LYBT.Shared.Models.Contracts.Common;
using System;
using Prism.Events;
using LYBT.Desktop.Core.Models.Patients;

namespace LYBT.Desktop.Core.Events
{
    /// <summary>
    /// 患者选择事件
    /// </summary>
    public class PatientSelectedEvent : PubSubEvent<PatientInfo>
    {
    }

    /// <summary>
    /// 诊疗会话开始事件
    /// </summary>
    public class ConsultationSessionStartedEvent : PubSubEvent<ConsultationSessionData>
    {
    }

    /// <summary>
    /// 中医四诊完成事件
    /// </summary>
    public class TCMFourDiagnosisCompletedEvent : PubSubEvent<TCMFourDiagnosisData>
    {
    }

    /// <summary>
    /// 处方创建事件
    /// </summary>
    public class PrescriptionCreatedEvent : PubSubEvent<PrescriptionCreatedData>
    {
    }

    /// <summary>
    /// 诊疗完成事件
    /// </summary>
    public class ConsultationCompletedEvent : PubSubEvent<ConsultationCompletedData>
    {
    }

    /// <summary>
    /// 诊疗流程导航事件
    /// </summary>
    public class ConsultationNavigationEvent : PubSubEvent<ConsultationNavigationData>
    {
    }

    /// <summary>
    /// 四诊保存事件
    /// </summary>
    public class FourDiagnosisSavedEvent : PubSubEvent<TCMFourDiagnosisData>
    {
    }

    /// <summary>
    /// 诊断保存事件
    /// </summary>
    public class DiagnosisSavedEvent : PubSubEvent<DiagnosisSavedEventArgs>
    {
    }

    /// <summary>
    /// 导入历史数据事件
    /// </summary>
    public class ImportHistoryDataEvent : PubSubEvent<object>
    {
    }

    /// <summary>
    /// 诊疗数据更新事件
    /// </summary>
    public class ConsultationDataUpdatedEvent : PubSubEvent<object>
    {
    }

    /// <summary>
    /// 数据刷新请求事件
    /// </summary>
    public class DataRefreshRequestEvent : PubSubEvent<DataRefreshRequestEventArgs>
    {
    }

    /// <summary>
    /// 错误发生事件
    /// </summary>
    public class ErrorOccurredEvent : PubSubEvent<ConsultationErrorEventArgs>
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

    #region 事件数据模型

    /// <summary>
    /// 诊疗会话数据
    /// </summary>
    public class ConsultationSessionData
    {
        public Guid PatientId { get; set; }
        public Guid MedicalCaseId { get; set; }
        public Guid ConsultationId { get; set; }
        public DateTime SessionStartTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 中医四诊数据
    /// </summary>
    public class TCMFourDiagnosisData
    {
        public string Diagnosis { get; set; } = string.Empty;
        public string InspectionResult { get; set; } = string.Empty;
        public string AuscultationResult { get; set; } = string.Empty;
        public string InquiryResult { get; set; } = string.Empty;
        public string PalpationResult { get; set; } = string.Empty;
        public string Syndrome { get; set; } = string.Empty;
        public string TreatmentPrinciple { get; set; } = string.Empty;
    }

    /// <summary>
    /// 处方创建数据
    /// </summary>
    public class PrescriptionCreatedData
    {
        public Guid PrescriptionId { get; set; }
        public Guid PatientId { get; set; }
        public Guid MedicalCaseId { get; set; }
        public Guid ConsultationId { get; set; }
        public string PrescriptionNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    /// <summary>
    /// 诊疗完成数据
    /// </summary>
    public class ConsultationCompletedData
    {
        public Guid ConsultationId { get; set; }
        public Guid PrescriptionId { get; set; }
        public Guid PatientId { get; set; }
        public DateTime CompletedTime { get; set; } = DateTime.Now;
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 诊疗流程导航数据
    /// </summary>
    public class ConsultationNavigationData
    {
        public ConsultationStep CurrentStep { get; set; }
        public ConsultationStep? NextStep { get; set; }
        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
    }

    /// <summary>
    /// 诊断保存事件参数
    /// </summary>
    public class DiagnosisSavedEventArgs
    {
        public string Diagnosis { get; set; } = string.Empty;
        public string Syndrome { get; set; } = string.Empty;
        public string TreatmentPrinciple { get; set; } = string.Empty;
        public string DifferentiationAnalysis { get; set; } = string.Empty;
        public DateTime SaveTime { get; set; } = DateTime.Now;
        public DateTime DiagnosisTime { get; set; } = DateTime.Now;
    }



    #endregion

    #region 枚举

    /// <summary>
    /// 诊疗流程步骤
    /// </summary>
    public enum ConsultationStep
    {
        /// <summary>患者选择</summary>
        PatientSelection,
        
        /// <summary>中医四诊</summary>
        TCMFourDiagnosis,
        
        /// <summary>辨证论治</summary>
        Differentiation,
        
        /// <summary>处方开具</summary>
        Prescription,
        
        /// <summary>完成确认</summary>
        Completion
    }



    #endregion
}