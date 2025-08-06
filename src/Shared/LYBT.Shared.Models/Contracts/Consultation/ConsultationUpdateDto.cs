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
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [StringLength(1000, ErrorMessage = "现病史长度不能超过1000个字符")]
        public string? PresentIllness { get; set; }

        /// <summary>既往史</summary>
        [StringLength(500, ErrorMessage = "既往史长度不能超过500个字符")]
        public string? PastHistory { get; set; }

        /// <summary>过敏史</summary>
        [StringLength(200, ErrorMessage = "过敏史长度不能超过200个字符")]
        public string? AllergyHistory { get; set; }

        /// <summary>体格检查</summary>
        [StringLength(1000, ErrorMessage = "体格检查长度不能超过1000个字符")]
        public string? PhysicalExamination { get; set; }

        /// <summary>舌诊</summary>
        [StringLength(200, ErrorMessage = "舌诊长度不能超过200个字符")]
        public string? TongueInspection { get; set; }

        /// <summary>脉诊</summary>
        [StringLength(200, ErrorMessage = "脉诊长度不能超过200个字符")]
        public string? PulseCondition { get; set; }

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