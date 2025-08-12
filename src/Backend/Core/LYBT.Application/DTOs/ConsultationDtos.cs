using System;
using System.Collections.Generic;
using LYBT.Domain.ValueObjects;
using LYBT.Domain.Aggregates.ConsultationAggregate;
using LYBT.Domain.Services;

namespace LYBT.Application.DTOs
{
    #region 看诊DTOs

    /// <summary>
    /// 看诊DTO
    /// </summary>
    public class ConsultationDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; }
        public DateTime ConsultationDate { get; set; }
        public string Status { get; set; }
        public string ChiefComplaint { get; set; }
        public Guid? MedicalCaseId { get; set; }
        public string MedicalCaseNo { get; set; }
    }

    /// <summary>
    /// 四诊信息DTO
    /// </summary>
    public class FourDiagnosisDto
    {
        public Guid ConsultationId { get; set; }
        public InspectionDto Inspection { get; set; }
        public AuscultationOlfactionDto AuscultationOlfaction { get; set; }
        public InquiryDto Inquiry { get; set; }
        public PalpationDto Palpation { get; set; }
    }

    /// <summary>
    /// 望诊DTO
    /// </summary>
    public class InspectionDto
    {
        public string Complexion { get; set; }
        public string Spirit { get; set; }
        public string BodyShape { get; set; }
        public string TongueCondition { get; set; }
        public string Observations { get; set; }
    }

    /// <summary>
    /// 闻诊DTO
    /// </summary>
    public class AuscultationOlfactionDto
    {
        public string Voice { get; set; }
        public string Breathing { get; set; }
        public string Cough { get; set; }
        public string Odor { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// 问诊DTO
    /// </summary>
    public class InquiryDto
    {
        public string ColdHeat { get; set; }
        public string Perspiration { get; set; }
        public string HeadBody { get; set; }
        public string Stool { get; set; }
        public string Urine { get; set; }
        public string Appetite { get; set; }
        public string ChestAbdomen { get; set; }
        public string Sleep { get; set; }
        public string Menstruation { get; set; }
        public string OtherSymptoms { get; set; }
    }

    /// <summary>
    /// 切诊DTO
    /// </summary>
    public class PalpationDto
    {
        public string PulseCondition { get; set; }
        public string AbdominalPalpation { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// 诊断DTO
    /// </summary>
    public class DiagnosisDto
    {
        public Guid ConsultationId { get; set; }
        public string TCMDisease { get; set; }
        public string TCMSyndrome { get; set; }
        public string SyndromeAnalysis { get; set; }
        public string TreatmentPrinciple { get; set; }
        public string DiseaseCode { get; set; }
    }

    /// <summary>
    /// 看诊总结DTO
    /// </summary>
    public class ConsultationSummaryDto
    {
        public Guid ConsultationId { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public DateTime ConsultationDate { get; set; }
        public string ChiefComplaint { get; set; }
        public string FourDiagnosisSummary { get; set; }
        public string Diagnosis { get; set; }
        public string TreatmentPlan { get; set; }
        public string DoctorAdvice { get; set; }
        public DateTime? NextAppointment { get; set; }
        public string Status { get; set; }
        public string MedicalCaseNo { get; set; }
        public int TotalConsultations { get; set; }
        public int TotalPrescriptions { get; set; }
    }

    #endregion

    #region 诊疗历史DTOs

    /// <summary>
    /// 患者诊疗历史
    /// </summary>
    public class PatientConsultationHistory
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; }
        public int TotalConsultations { get; set; }
        public int ActiveMedicalCases { get; set; }
        public List<ConsultationSummaryItem> RecentConsultations { get; set; }
        public List<FrequentDiagnosis> FrequentDiagnoses { get; set; }
        public List<string> Allergies { get; set; }
        public List<string> Contraindications { get; set; }

        public PatientConsultationHistory()
        {
            RecentConsultations = new List<ConsultationSummaryItem>();
            FrequentDiagnoses = new List<FrequentDiagnosis>();
            Allergies = new List<string>();
            Contraindications = new List<string>();
        }
    }

    /// <summary>
    /// 看诊摘要项
    /// </summary>
    public class ConsultationSummaryItem
    {
        public Guid ConsultationId { get; set; }
        public DateTime ConsultationDate { get; set; }
        public string DoctorName { get; set; }
        public string ChiefComplaint { get; set; }
        public string Diagnosis { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// 常见诊断
    /// </summary>
    public class FrequentDiagnosis
    {
        public string DiseaseName { get; set; }
        public int Count { get; set; }
        public DateTime LastOccurrence { get; set; }
    }

    #endregion

    #region 诊疗建议DTOs

    /// <summary>
    /// 诊疗建议
    /// </summary>
    public class ConsultationRecommendations
    {
        public Guid ConsultationId { get; set; }
        public List<FormulaRecommendation> RecommendedFormulas { get; set; }
        public List<SimilarCase> SimilarCases { get; set; }
        public List<string> TreatmentSuggestions { get; set; }
        public List<string> LifestyleAdvice { get; set; }

        public ConsultationRecommendations()
        {
            RecommendedFormulas = new List<FormulaRecommendation>();
            SimilarCases = new List<SimilarCase>();
            TreatmentSuggestions = new List<string>();
            LifestyleAdvice = new List<string>();
        }
    }

    /// <summary>
    /// 相似病例
    /// </summary>
    public class SimilarCase
    {
        public Guid MedicalCaseId { get; set; }
        public string CaseNo { get; set; }
        public string PatientInfo { get; set; }
        public string Diagnosis { get; set; }
        public string Syndrome { get; set; }
        public string Treatment { get; set; }
        public string Outcome { get; set; }
        public decimal SimilarityScore { get; set; }
    }

    #endregion
}