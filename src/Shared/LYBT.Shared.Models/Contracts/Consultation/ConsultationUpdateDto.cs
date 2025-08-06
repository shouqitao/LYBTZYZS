using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 更新看诊记录DTO
    /// </summary>
    public class ConsultationUpdateDto
    {
        /// <summary>主诉</summary>
        [StringLength(500, ErrorMessage = "主诉长度不能超过500个字符")]
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [StringLength(1000, ErrorMessage = "现病史长度不能超过1000个字符")]
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>既往史</summary>
        [StringLength(500, ErrorMessage = "既往史长度不能超过500个字符")]
        [DisplayName("既往史")]
        public string? PastHistory { get; set; }

        /// <summary>过敏史</summary>
        [StringLength(200, ErrorMessage = "过敏史长度不能超过200个字符")]
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>体格检查</summary>
        [StringLength(1000, ErrorMessage = "体格检查长度不能超过1000个字符")]
        [DisplayName("体格检查")]
        public string? PhysicalExamination { get; set; }

        /// <summary>望诊</summary>
        [StringLength(500, ErrorMessage = "望诊长度不能超过500个字符")]
        [DisplayName("望诊")]
        public string? Inspection { get; set; }

        /// <summary>闻诊</summary>
        [StringLength(500, ErrorMessage = "闻诊长度不能超过500个字符")]
        [DisplayName("闻诊")]
        public string? AuscultationOlfaction { get; set; }

        /// <summary>问诊</summary>
        [StringLength(1000, ErrorMessage = "问诊长度不能超过1000个字符")]
        [DisplayName("问诊")]
        public string? Inquiry { get; set; }

        /// <summary>切诊</summary>
        [StringLength(500, ErrorMessage = "切诊长度不能超过500个字符")]
        [DisplayName("切诊")]
        public string? Palpation { get; set; }

        /// <summary>舌诊</summary>
        [StringLength(200, ErrorMessage = "舌诊长度不能超过200个字符")]
        [DisplayName("舌诊")]
        public string? TongueInspection { get; set; }

        /// <summary>脉诊</summary>
        [StringLength(200, ErrorMessage = "脉诊长度不能超过200个字符")]
        [DisplayName("脉诊")]
        public string? PulseCondition { get; set; }

        /// <summary>体温</summary>
        [Range(35.0, 42.0, ErrorMessage = "体温应在35-42度之间")]
        [DisplayName("体温")]
        public decimal? Temperature { get; set; }

        /// <summary>血压（收缩压）</summary>
        [Range(60, 250, ErrorMessage = "收缩压应在60-250之间")]
        [DisplayName("收缩压")]
        public int? SystolicPressure { get; set; }

        /// <summary>血压（舒张压）</summary>
        [Range(40, 150, ErrorMessage = "舒张压应在40-150之间")]
        [DisplayName("舒张压")]
        public int? DiastolicPressure { get; set; }

        /// <summary>心率</summary>
        [Range(30, 200, ErrorMessage = "心率应在30-200之间")]
        [DisplayName("心率")]
        public int? HeartRate { get; set; }

        /// <summary>呼吸频率</summary>
        [Range(8, 40, ErrorMessage = "呼吸频率应在8-40之间")]
        [DisplayName("呼吸频率")]
        public int? RespiratoryRate { get; set; }

        /// <summary>中医辨证</summary>
        [StringLength(500, ErrorMessage = "中医辨证长度不能超过500个字符")]
        public string? TCMDiagnosis { get; set; }

        /// <summary>西医诊断</summary>
        [StringLength(500, ErrorMessage = "西医诊断长度不能超过500个字符")]
        public string? WesternDiagnosis { get; set; }

        /// <summary>诊断（综合）</summary>
        [StringLength(500, ErrorMessage = "诊断长度不能超过500个字符")]
        public string? Diagnosis { get; set; }

        /// <summary>治疗原则</summary>
        [StringLength(200, ErrorMessage = "治疗原则长度不能超过200个字符")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        [StringLength(500, ErrorMessage = "医嘱长度不能超过500个字符")]
        public string? MedicalAdvice { get; set; }

        /// <summary>治疗方案ID</summary>
        public Guid? TreatmentPlanId { get; set; }
    }
}