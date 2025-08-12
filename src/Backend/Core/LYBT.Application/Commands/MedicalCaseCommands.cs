using System;
using System.Collections.Generic;
using LYBT.Domain.Aggregates.MedicalCaseAggregate;
using LYBT.Domain.ValueObjects;

namespace LYBT.Application.Commands
{
    #region 创建和更新病案命令

    /// <summary>
    /// 创建病案命令
    /// </summary>
    public class CreateMedicalCaseCommand
    {
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; }
        public CaseType CaseType { get; set; }
        public bool IsEmergency { get; set; }
        public bool ForceCreate { get; set; }
        
        // 主诉
        public string ChiefComplaint { get; set; }
        public int DurationDays { get; set; }
        public string Severity { get; set; }
        
        // 现病史
        public string PresentIllness { get; set; }
        public string Onset { get; set; }
        public string Development { get; set; }
        public string CurrentStatus { get; set; }
        public string TreatmentHistory { get; set; }
        
        // 既往史
        public PastHistoryCommand PastHistory { get; set; }
        
        // 转诊信息
        public bool IsReferral { get; set; }
        public Guid? ReferredFromDoctorId { get; set; }
        public string ReferralReason { get; set; }
    }

    /// <summary>
    /// 既往史命令
    /// </summary>
    public class PastHistoryCommand
    {
        public List<string> Diseases { get; set; }
        public List<string> Surgeries { get; set; }
        public List<string> Allergies { get; set; }
        public List<string> Medications { get; set; }
    }

    /// <summary>
    /// 更新病史命令
    /// </summary>
    public class UpdateMedicalHistoryCommand
    {
        public Guid MedicalCaseId { get; set; }
        public PersonalHistoryCommand PersonalHistory { get; set; }
        public List<string> FamilyHistory { get; set; }
    }

    /// <summary>
    /// 个人史命令
    /// </summary>
    public class PersonalHistoryCommand
    {
        public string Occupation { get; set; }
        public string Lifestyle { get; set; }
        public string DietaryHabits { get; set; }
        public string SmokingHistory { get; set; }
        public string DrinkingHistory { get; set; }
    }

    /// <summary>
    /// 完成病案命令
    /// </summary>
    public class CompleteMedicalCaseCommand
    {
        public Guid MedicalCaseId { get; set; }
        public TreatmentOutcomeCommand Outcome { get; set; }
        public string Prognosis { get; set; }
        public FollowUpPlanCommand FollowUpPlan { get; set; }
        public string Summary { get; set; }
    }

    /// <summary>
    /// 治疗结果命令
    /// </summary>
    public class TreatmentOutcomeCommand
    {
        public TreatmentEffect Effect { get; set; }
        public string Symptoms { get; set; }
        public string Signs { get; set; }
        public string LabResults { get; set; }
        public string Complications { get; set; }
    }

    /// <summary>
    /// 随访计划命令
    /// </summary>
    public class FollowUpPlanCommand
    {
        public DateTime FollowUpDate { get; set; }
        public string Method { get; set; }
        public string Advice { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
    }

    #endregion

    #region 诊疗记录命令

    /// <summary>
    /// 添加检查命令
    /// </summary>
    public class AddExaminationCommand
    {
        public Guid MedicalCaseId { get; set; }
        public string ExaminationType { get; set; }
        public string ExaminationItem { get; set; }
        public DateTime ExaminationDate { get; set; }
        public string Result { get; set; }
        public string Conclusion { get; set; }
    }

    /// <summary>
    /// 添加治疗命令
    /// </summary>
    public class AddTreatmentCommand
    {
        public Guid MedicalCaseId { get; set; }
        public string TreatmentType { get; set; }
        public string TreatmentMethod { get; set; }
        public DateTime TreatmentDate { get; set; }
        public string TreatmentDetails { get; set; }
        public string Effect { get; set; }
        public Money Cost { get; set; }
    }

    /// <summary>
    /// 添加病程记录命令
    /// </summary>
    public class AddProgressNoteCommand
    {
        public Guid MedicalCaseId { get; set; }
        public DateTime RecordDate { get; set; }
        public string Symptoms { get; set; }
        public string Signs { get; set; }
        public string Assessment { get; set; }
        public string Plan { get; set; }
        public Guid RecordedBy { get; set; }
        public string RecorderName { get; set; }
    }

    /// <summary>
    /// 记录随访命令
    /// </summary>
    public class RecordFollowUpCommand
    {
        public Guid MedicalCaseId { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string Method { get; set; }
        public string Status { get; set; }
        public string Symptoms { get; set; }
        public string Medication { get; set; }
        public string Advice { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
    }

    #endregion
}