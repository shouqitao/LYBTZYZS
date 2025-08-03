using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.Users;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Core.Models.Consultation
{
    /// <summary>
    /// 诊疗记录信息模型
    /// </summary>
    public class ConsultationRecord
    {
        /// <summary>诊疗记录ID</summary>
        public Guid Id { get; set; }

        /// <summary>患者信息</summary>
        public PatientInfo Patient { get; set; } = new();

        /// <summary>医生信息</summary>
        public UserInfo Doctor { get; set; } = new();

        /// <summary>就诊日期</summary>
        public DateTime ConsultationDate { get; set; }

        /// <summary>主诉</summary>
        public string ChiefComplaint { get; set; } = string.Empty;

        /// <summary>现病史</summary>
        public string PresentIllness { get; set; } = string.Empty;

        /// <summary>既往史</summary>
        public string PastHistory { get; set; } = string.Empty;

        /// <summary>体格检查</summary>
        public string PhysicalExamination { get; set; } = string.Empty;

        /// <summary>中医四诊</summary>
        public TCMDiagnosis TCMDiagnosis { get; set; } = new();

        /// <summary>西医诊断</summary>
        public string WesternDiagnosis { get; set; } = string.Empty;

        /// <summary>中医诊断</summary>
        public string TCMDiagnosisResult { get; set; } = string.Empty;

        /// <summary>治疗原则</summary>
        public string TreatmentPrinciple { get; set; } = string.Empty;

        /// <summary>处方信息</summary>
        public List<PrescriptionItem> Prescription { get; set; } = new();

        /// <summary>医嘱</summary>
        public string DoctorAdvice { get; set; } = string.Empty;

        /// <summary>复诊时间</summary>
        public DateTime? FollowUpDate { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>诊疗状态</summary>
        public ConsultationStatus Status { get; set; } = ConsultationStatus.InProgress;

        /// <summary>处方总价</summary>
        public decimal TotalAmount => Prescription?.Sum(p => p.Amount) ?? 0;

        /// <summary>状态文本</summary>
        public string StatusText => Status switch
        {
            ConsultationStatus.InProgress => "诊疗中",
            ConsultationStatus.Completed => "已完成",
            ConsultationStatus.Cancelled => "已取消",
            _ => "未知"
        };

        /// <summary>医生ID</summary>
        public Guid DoctorId { get; set; }

        /// <summary>医生姓名</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>更新时间</summary>
        public DateTime? UpdatedTime { get; set; }

        /// <summary>
        /// 转换为病历记录
        /// </summary>
        public MedicalRecord ToMedicalRecord()
        {
            return new MedicalRecord
            {
                Id = Id,
                PatientId = Patient.Id.ToString(),
                PatientName = Patient.Name,
                Patient = Patient,
                Diagnosis = !string.IsNullOrEmpty(TCMDiagnosisResult) ? TCMDiagnosisResult : WesternDiagnosis ?? "",
                ChiefComplaint = ChiefComplaint,
                PresentIllness = PresentIllness,
                PastHistory = PastHistory,
                PhysicalExamination = PhysicalExamination,
                TCMDiagnosisResult = TCMDiagnosisResult,
                WesternDiagnosis = WesternDiagnosis,
                TreatmentAdvice = DoctorAdvice,
                TreatmentPrinciple = TreatmentPrinciple,
                TCMDiagnosis = TCMDiagnosis,
                HerbalFormula = Prescription.Select(p => new HerbItem
                {
                    HerbId = p.Herb.Id,
                    HerbName = p.Herb.Name,
                    Dosage = p.Dosage,
                    Unit = p.Unit,
                    UnitPrice = p.UnitPrice,
                    Usage = p.Usage,
                    Remark = p.Remark
                }).ToList(),
                CreatedBy = DoctorId.ToString(),
                CreatedTime = CreatedTime,
                RecordTime = DateTime.Now
            };
        }
    }

    /// <summary>
    /// 中医四诊信息
    /// </summary>
    public class TCMDiagnosis
    {
        /// <summary>望诊</summary>
        public string? Inspection { get; set; }

        /// <summary>闻诊</summary>
        public string? Auscultation { get; set; }

        /// <summary>问诊</summary>
        public string? Inquiry { get; set; }

        /// <summary>切诊(脉象)</summary>
        public string? Palpation { get; set; }

        /// <summary>舌象</summary>
        public string? TongueExamination { get; set; }
    }

    /// <summary>
    /// 处方项目
    /// </summary>
    public class PrescriptionItem
    {
        /// <summary>药材信息</summary>
        public HerbInfo Herb { get; set; } = new();

        /// <summary>剂量</summary>
        public decimal Dosage { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = "g";

        /// <summary>用法</summary>
        public string Usage { get; set; } = string.Empty;

        /// <summary>单价</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>总价</summary>
        public decimal Amount => Dosage * UnitPrice;

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }


    /// <summary>
    /// 病历记录模型（对应后端Record）
    /// </summary>
    public class MedicalRecord
    {
        /// <summary>病历ID</summary>
        public Guid Id { get; set; }

        /// <summary>患者ID</summary>
        public string PatientId { get; set; } = string.Empty;

        /// <summary>患者姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>患者信息</summary>
        public PatientInfo? Patient { get; set; }

        /// <summary>挂号ID（可选，因为支持直接看诊）</summary>
        public Guid? RegistrationId { get; set; }

        /// <summary>诊断内容</summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>主诉</summary>
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        public string? PresentIllness { get; set; }

        /// <summary>既往史</summary>
        public string? PastHistory { get; set; }

        /// <summary>体格检查</summary>
        public string? PhysicalExamination { get; set; }

        /// <summary>中医四诊</summary>
        public TCMDiagnosis TCMDiagnosis { get; set; } = new();

        /// <summary>中医诊断结果</summary>
        public string? TCMDiagnosisResult { get; set; }

        /// <summary>西医诊断</summary>
        public string? WesternDiagnosis { get; set; }

        /// <summary>诊疗建议</summary>
        public string? TreatmentAdvice { get; set; }

        /// <summary>治疗原则</summary>
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        public string? DoctorAdvice { get; set; }

        /// <summary>复诊时间</summary>
        public DateTime? FollowUpDate { get; set; }

        /// <summary>处方ID</summary>
        public Guid? PrescriptionId { get; set; }

        /// <summary>辩证结果列表</summary>
        public List<string> DiagnosisResults { get; set; } = new();

        /// <summary>药材组成</summary>
        public List<HerbItem> HerbalFormula { get; set; } = new();

        /// <summary>辅助治疗方案</summary>
        public List<TreatmentItem> TreatmentPlans { get; set; } = new();

        /// <summary>是否共享</summary>
        public bool IsShared { get; set; }

        /// <summary>共享给医生ID列表</summary>
        public List<string> SharedToDoctorIds { get; set; } = new();

        /// <summary>创建医生ID</summary>
        public string? CreatedBy { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>病历记录时间</summary>
        public DateTime RecordTime { get; set; } = DateTime.Now;

        /// <summary>处方总价</summary>
        public decimal TotalAmount => HerbalFormula.Sum(h => h.Amount);

        /// <summary>病历状态</summary>
        public RecordStatus Status { get; set; } = RecordStatus.InProgress;
    }

    /// <summary>
    /// 药材条目（后端数据结构）
    /// </summary>
    public class HerbItem
    {
        /// <summary>药材ID</summary>
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>剂量</summary>
        public decimal Dosage { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = "g";

        /// <summary>单价</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>金额</summary>
        public decimal Amount => Dosage * UnitPrice;

        /// <summary>用法</summary>
        public string? Usage { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 治疗项目
    /// </summary>
    public class TreatmentItem
    {
        /// <summary>治疗项目名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>治疗说明</summary>
        public string? Description { get; set; }

        /// <summary>费用</summary>
        public decimal Fee { get; set; }

        /// <summary>执行时间</summary>
        public DateTime? ExecuteTime { get; set; }
    }

}