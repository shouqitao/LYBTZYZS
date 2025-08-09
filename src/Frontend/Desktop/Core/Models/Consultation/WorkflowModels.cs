using System;
using System.Collections.Generic;

namespace LYBT.WPF.Client.Core.Models.Consultation
{
    /// <summary>
    /// 工作流步骤枚举
    /// </summary>
    public enum WorkflowStep
    {
        PatientSelection,
        FourDiagnosis,
        Differentiation,
        Prescription
    }

    /// <summary>
    /// 工作流步骤数据
    /// </summary>
    public class WorkflowStepData
    {
        public WorkflowStep Step { get; set; }
        public object? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 四诊数据
    /// </summary>
    public class FourDiagnosisData
    {
        public string Inspection { get; set; } = string.Empty;
        public string Auscultation { get; set; } = string.Empty;
        public string Inquiry { get; set; } = string.Empty;
        public string Palpation { get; set; } = string.Empty;
        public string? ImportSource { get; set; }
    }

    /// <summary>
    /// 辨证数据
    /// </summary>
    public class DifferentiationData
    {
        public string Syndrome { get; set; } = string.Empty;
        public string TreatmentPrinciple { get; set; } = string.Empty;
        public string Analysis { get; set; } = string.Empty;
    }

    /// <summary>
    /// 处方数据
    /// </summary>
    public class PrescriptionData
    {
        public List<PrescriptionItem> Items { get; set; } = new();
        public int Dosage { get; set; }
        public string Usage { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public decimal Discount { get; set; } = 1.0m;
    }

    /// <summary>
    /// 处方项
    /// </summary>
    public class PrescriptionItem
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "g";
        public decimal UnitPrice { get; set; }
        public string? ImportSource { get; set; }
    }

    /// <summary>
    /// 诊疗数据
    /// </summary>
    public class ConsultationData
    {
        public Guid MedicalCaseId { get; set; }
        public Guid PatientId { get; set; }
        public FourDiagnosisData? FourDiagnosis { get; set; }
        public DifferentiationData? Differentiation { get; set; }
        public PrescriptionData? Prescription { get; set; }
        public string? Diagnosis { get; set; }
        public ConsultationStatus Status { get; set; } = ConsultationStatus.Draft;
    }
    
    /// <summary>
    /// 诊疗状态
    /// </summary>
    public enum ConsultationStatus
    {
        Draft,      // 草稿
        InProgress, // 进行中
        Completed   // 已完成
    }
}