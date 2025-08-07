using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Core
{
    /// <summary>
    /// 看诊基础模型 - 前后端共享核心字段
    /// 包含中医四诊、西医检查、诊断治疗等完整看诊信息
    /// </summary>
    public class BaseConsultationModel
    {
        /// <summary>看诊ID</summary>
        [DisplayName("看诊ID")]
        public Guid Id { get; set; }

        /// <summary>医疗案例ID</summary>
        [DisplayName("医疗案例ID")]
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>关联用户ID（医生）</summary>
        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        /// <summary>主诉</summary>
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>既往史</summary>
        [DisplayName("既往史")]
        public string? PastHistory { get; set; }

        /// <summary>过敏史</summary>
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>体格检查</summary>
        [DisplayName("体格检查")]
        public string? PhysicalExamination { get; set; }

        /// <summary>望诊</summary>
        [DisplayName("望诊")]
        public string? Inspection { get; set; }

        /// <summary>闻诊</summary>
        [DisplayName("闻诊")]
        public string? AuscultationOlfaction { get; set; }

        /// <summary>问诊</summary>
        [DisplayName("问诊")]
        public string? Inquiry { get; set; }

        /// <summary>切诊</summary>
        [DisplayName("切诊")]
        public string? Palpation { get; set; }

        /// <summary>舌诊</summary>
        [DisplayName("舌诊")]
        public string? TongueInspection { get; set; }

        /// <summary>脉诊</summary>
        [DisplayName("脉诊")]
        public string? PulseCondition { get; set; }

        /// <summary>体温</summary>
        [DisplayName("体温")]
        public decimal? Temperature { get; set; }

        /// <summary>血压（收缩压）</summary>
        [DisplayName("收缩压")]
        public int? SystolicPressure { get; set; }

        /// <summary>血压（舒张压）</summary>
        [DisplayName("舒张压")]
        public int? DiastolicPressure { get; set; }

        /// <summary>心率</summary>
        [DisplayName("心率")]
        public int? HeartRate { get; set; }

        /// <summary>呼吸频率</summary>
        [DisplayName("呼吸频率")]
        public int? RespiratoryRate { get; set; }

        /// <summary>中医辨证</summary>
        [DisplayName("中医辨证")]
        public string? TCMDiagnosis { get; set; }

        /// <summary>西医诊断</summary>
        [DisplayName("西医诊断")]
        public string? WesternDiagnosis { get; set; }

        /// <summary>诊断（综合）</summary>
        [DisplayName("诊断")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>诊断类型ID</summary>
        [DisplayName("诊断类型ID")]
        public Guid? DiagnosisCatalogId { get; set; }

        /// <summary>治疗原则</summary>
        [DisplayName("治疗原则")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        /// <summary>看诊时间</summary>
        [DisplayName("看诊时间")]
        public DateTime ConsultationTime { get; set; } = DateTime.Now;

        /// <summary>看诊时长（分钟）</summary>
        [DisplayName("看诊时长")]
        public int? Duration { get; set; }

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    }
}