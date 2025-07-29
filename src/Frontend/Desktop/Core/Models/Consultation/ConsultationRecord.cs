using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.Users;
using LYBT.WPF.Client.Core.Models.Herbs;

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
    }

    /// <summary>
    /// 中医四诊信息
    /// </summary>
    public class TCMDiagnosis
    {
        /// <summary>望诊</summary>
        public string Inspection { get; set; } = string.Empty;

        /// <summary>闻诊</summary>
        public string Auscultation { get; set; } = string.Empty;

        /// <summary>问诊</summary>
        public string Inquiry { get; set; } = string.Empty;

        /// <summary>切诊(脉象)</summary>
        public string Palpation { get; set; } = string.Empty;

        /// <summary>舌象</summary>
        public string TongueExamination { get; set; } = string.Empty;
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
    }

    /// <summary>
    /// 诊疗状态枚举
    /// </summary>
    public enum ConsultationStatus
    {
        /// <summary>诊疗中</summary>
        InProgress = 1,

        /// <summary>已完成</summary>
        Completed = 2,

        /// <summary>已取消</summary>
        Cancelled = 3
    }
}