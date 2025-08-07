using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 完成看诊DTO
    /// </summary>
    public class ConsultationCompleteDto
    {
        /// <summary>诊断（综合）</summary>
        [Required(ErrorMessage = "诊断不能为空")]
        [StringLength(500, ErrorMessage = "诊断长度不能超过500个字符")]
        [DisplayName("诊断")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>中医辨证</summary>
        [StringLength(500, ErrorMessage = "中医辨证长度不能超过500个字符")]
        [DisplayName("中医辨证")]
        public string? TCMDiagnosis { get; set; }

        /// <summary>治疗原则</summary>
        [StringLength(200, ErrorMessage = "治疗原则长度不能超过200个字符")]
        [DisplayName("治疗原则")]
        public string? TreatmentPrinciple { get; set; }

        /// <summary>医嘱</summary>
        [StringLength(500, ErrorMessage = "医嘱长度不能超过500个字符")]
        [DisplayName("医嘱")]
        public string? MedicalAdvice { get; set; }

        /// <summary>治疗方案ID</summary>
        [DisplayName("治疗方案ID")]
        public Guid? TreatmentPlanId { get; set; }

        /// <summary>是否需要复诊</summary>
        [DisplayName("是否需要复诊")]
        public bool NeedFollowUp { get; set; }

        /// <summary>复诊日期</summary>
        [DisplayName("复诊日期")]
        public DateTime? FollowUpDate { get; set; }

        /// <summary>复诊备注</summary>
        [StringLength(200, ErrorMessage = "复诊备注长度不能超过200个字符")]
        [DisplayName("复诊备注")]
        public string? FollowUpRemark { get; set; }
    }
}