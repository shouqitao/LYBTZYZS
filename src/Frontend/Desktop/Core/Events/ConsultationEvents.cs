using System;
using Prism.Events;
using LYBT.WPF.Client.Core.Models.Consultation;

namespace LYBT.WPF.Client.Core.Events
{
    /// <summary>
    /// 诊疗数据更新事件
    /// </summary>
    public class ConsultationDataUpdatedEvent : PubSubEvent<ConsultationData>
    {
    }

    /// <summary>
    /// 导入历史数据事件
    /// </summary>
    public class ImportHistoryDataEvent : PubSubEvent<ImportHistoryDataEventArgs>
    {
    }

    /// <summary>
    /// 导入历史数据事件参数
    /// </summary>
    public class ImportHistoryDataEventArgs
    {
        public Guid SourceMedicalCaseId { get; set; }
        public string DataType { get; set; } = "";  // FourDiagnosis, Diagnosis, Prescription
        public WorkflowStep TargetStep { get; set; }
    }

    /// <summary>
    /// 工作流导航事件
    /// </summary>
    public class WorkflowNavigationEvent : PubSubEvent<WorkflowNavigationEventArgs>
    {
    }

    /// <summary>
    /// 工作流导航事件参数
    /// </summary>
    public class WorkflowNavigationEventArgs
    {
        public WorkflowStep FromStep { get; set; }
        public WorkflowStep ToStep { get; set; }
        public bool IsForward { get; set; }
    }

    /// <summary>
    /// 患者选择完成事件
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
        public string PatientName { get; set; } = "";
        public string Gender { get; set; } = "";
        public int Age { get; set; }
        public Guid MedicalCaseId { get; set; }
    }

    /// <summary>
    /// 四诊数据保存事件
    /// </summary>
    public class FourDiagnosisSavedEvent : PubSubEvent<FourDiagnosisData>
    {
    }

    /// <summary>
    /// 诊断保存事件
    /// </summary>
    public class DiagnosisSavedEvent : PubSubEvent<DiagnosisSavedEventArgs>
    {
    }

    /// <summary>
    /// 诊断保存事件参数
    /// </summary>
    public class DiagnosisSavedEventArgs
    {
        public string Diagnosis { get; set; } = "";
        public string DifferentiationAnalysis { get; set; } = "";
        public DateTime DiagnosisTime { get; set; }
    }

    /// <summary>
    /// 处方保存事件
    /// </summary>
    public class PrescriptionSavedEvent : PubSubEvent<PrescriptionData>
    {
    }

    /// <summary>
    /// 工作流完成事件参数
    /// </summary>
    public class WorkflowCompletedEventArgs
    {
        public Guid MedicalCaseId { get; set; }
        public Guid PatientId { get; set; }
        public DateTime CompletedTime { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}