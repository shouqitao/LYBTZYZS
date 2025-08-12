using System;
using System.Collections.Generic;
using LYBT.Domain.ValueObjects;
using LYBT.Domain.Aggregates.MedicalCaseAggregate;

namespace LYBT.Application.DTOs
{
    #region 病案基础DTOs

    /// <summary>
    /// 病案DTO
    /// </summary>
    public class MedicalCaseDto
    {
        public Guid Id { get; set; }
        public string CaseNo { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; }
        public DateTime AdmissionDate { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public bool IsEmergency { get; set; }
        public bool IsReferral { get; set; }
    }

    /// <summary>
    /// 病案摘要DTO
    /// </summary>
    public class MedicalCaseSummaryDto
    {
        public Guid Id { get; set; }
        public string CaseNo { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public DateTime AdmissionDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public string Status { get; set; }
        public string PrimaryDiagnosis { get; set; }
        public int ConsultationCount { get; set; }
        public string TotalCost { get; set; }
    }

    /// <summary>
    /// 病案详情DTO
    /// </summary>
    public class MedicalCaseDetailDto
    {
        public Guid Id { get; set; }
        public string CaseNo { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public int PatientAge { get; set; }
        public string PatientGender { get; set; }
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; }
        public DateTime AdmissionDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public bool IsEmergency { get; set; }
        public bool IsReferral { get; set; }
        public string ReferralReason { get; set; }
        
        // 病史信息
        public string ChiefComplaint { get; set; }
        public string PresentIllness { get; set; }
        public string PastHistory { get; set; }
        public string PersonalHistory { get; set; }
        public string FamilyHistory { get; set; }
        
        // 诊断信息
        public TCMDiagnosisDto TCMDiagnosis { get; set; }
        public List<DiagnosisRecordDto> Diagnoses { get; set; }
        
        // 诊疗记录统计
        public int ConsultationCount { get; set; }
        public int PrescriptionCount { get; set; }
        public int ExaminationCount { get; set; }
        public int TreatmentCount { get; set; }
        public int ProgressNoteCount { get; set; }
        
        // 治疗结果
        public TreatmentOutcomeDto Outcome { get; set; }
        public string Prognosis { get; set; }
        
        // 费用信息
        public string TotalCost { get; set; }
        
        // 随访信息
        public int FollowUpCount { get; set; }
        public bool NeedsFollowUp { get; set; }
    }

    /// <summary>
    /// 病案完成DTO
    /// </summary>
    public class MedicalCaseCompletionDto
    {
        public Guid MedicalCaseId { get; set; }
        public string CaseNo { get; set; }
        public DateTime CompletionDate { get; set; }
        public string Summary { get; set; }
        public int TreatmentDays { get; set; }
        public int ConsultationCount { get; set; }
        public int PrescriptionCount { get; set; }
        public string TotalCost { get; set; }
        public string Outcome { get; set; }
        public string Prognosis { get; set; }
        public bool NeedsFollowUp { get; set; }
    }

    #endregion

    #region 诊断相关DTOs

    /// <summary>
    /// 中医诊断DTO
    /// </summary>
    public class TCMDiagnosisDto
    {
        public string Disease { get; set; }
        public string Syndrome { get; set; }
        public string SyndromeAnalysis { get; set; }
        public string TreatmentPrinciple { get; set; }
    }

    /// <summary>
    /// 诊断记录DTO
    /// </summary>
    public class DiagnosisRecordDto
    {
        public string DiseaseName { get; set; }
        public string DiseaseCode { get; set; }
        public string Syndrome { get; set; }
        public string Type { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime DiagnosisDate { get; set; }
    }

    /// <summary>
    /// 治疗结果DTO
    /// </summary>
    public class TreatmentOutcomeDto
    {
        public string Effect { get; set; }
        public string Symptoms { get; set; }
        public string Signs { get; set; }
        public string LabResults { get; set; }
        public string Complications { get; set; }
    }

    #endregion

    #region 报告和统计DTOs

    /// <summary>
    /// 病案报告
    /// </summary>
    public class MedicalCaseReport
    {
        public string CaseNo { get; set; }
        public string PatientName { get; set; }
        public int PatientAge { get; set; }
        public string PatientGender { get; set; }
        public string DoctorName { get; set; }
        public DateTime AdmissionDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public int TreatmentDays { get; set; }
        public string Status { get; set; }
        
        // 病史
        public string ChiefComplaint { get; set; }
        public string PresentIllness { get; set; }
        public string PastHistory { get; set; }
        
        // 诊断
        public string PrimaryDiagnosis { get; set; }
        public List<string> SecondaryDiagnoses { get; set; }
        
        // 治疗统计
        public int ConsultationCount { get; set; }
        public int PrescriptionCount { get; set; }
        public int ExaminationCount { get; set; }
        public int TreatmentCount { get; set; }
        
        // 费用
        public string TotalCost { get; set; }
        public List<BillingSummary> BillingDetails { get; set; }
        
        // 结果
        public string TreatmentEffect { get; set; }
        public string Complications { get; set; }
        public string Prognosis { get; set; }
        
        // 随访
        public DateTime? NextFollowUpDate { get; set; }
        public string FollowUpAdvice { get; set; }
        
        // 摘要
        public string Summary { get; set; }
    }

    /// <summary>
    /// 费用摘要
    /// </summary>
    public class BillingSummary
    {
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// 病案统计
    /// </summary>
    public class MedicalCaseStatistics
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalCases { get; set; }
        public int ActiveCases { get; set; }
        public int CompletedCases { get; set; }
        public int ClosedCases { get; set; }
        public int EmergencyCases { get; set; }
        public int ReferralCases { get; set; }
        
        // 按类型统计
        public List<CaseTypeStatistic> CasesByType { get; set; }
        
        // 按诊断统计
        public List<DiagnosisStatistic> TopDiagnoses { get; set; }
        
        // 治疗效果统计
        public List<EffectivenessStatistic> TreatmentEffectiveness { get; set; }
        
        // 平均指标
        public double AverageTreatmentDays { get; set; }
        public double AverageConsultations { get; set; }
        public double AveragePrescriptions { get; set; }
        public decimal AverageCost { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    /// <summary>
    /// 病案类型统计
    /// </summary>
    public class CaseTypeStatistic
    {
        public string Type { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// 诊断统计
    /// </summary>
    public class DiagnosisStatistic
    {
        public string DiseaseName { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// 治疗效果统计
    /// </summary>
    public class EffectivenessStatistic
    {
        public string Effect { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    #endregion
}