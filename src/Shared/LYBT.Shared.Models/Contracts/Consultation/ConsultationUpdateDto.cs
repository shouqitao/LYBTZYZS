using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 更新看诊记录DTO - 纯中医版本
    /// </summary>
    public class ConsultationUpdateDto
    {

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

        /// <summary>中医辨证</summary>
        [StringLength(500, ErrorMessage = "中医辨证长度不能超过500个字符")]
        [DisplayName("中医辨证")]
        public string? TCMDiagnosis { get; set; }

        /// <summary>诊断（综合）</summary>
        [StringLength(500, ErrorMessage = "诊断长度不能超过500个字符")]
        public string? Diagnosis { get; set; }

        /// <summary>治疗原则</summary>
        [StringLength(200, ErrorMessage = "治疗原则长度不能超过200个字符")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        [StringLength(500, ErrorMessage = "医嘱长度不能超过500个字符")]
        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        /// <summary>备注信息</summary>
        [StringLength(1000, ErrorMessage = "备注信息长度不能超过1000个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}