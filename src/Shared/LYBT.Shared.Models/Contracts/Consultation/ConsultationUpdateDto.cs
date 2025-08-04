using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 更新看诊记录DTO
    /// </summary>
    public class ConsultationUpdateDto
    {
        /// <summary>看诊ID</summary>
        [Required(ErrorMessage = "看诊ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>主诉</summary>
        [Required(ErrorMessage = "主诉不能为空")]
        [StringLength(500, ErrorMessage = "主诉长度不能超过500个字符")]
        public string ChiefComplaint { get; set; } = string.Empty;

        /// <summary>现病史</summary>
        [StringLength(1000, ErrorMessage = "现病史长度不能超过1000个字符")]
        public string? PresentIllness { get; set; }

        /// <summary>体格检查</summary>
        [StringLength(1000, ErrorMessage = "体格检查长度不能超过1000个字符")]
        public string? PhysicalExamination { get; set; }

        /// <summary>诊断</summary>
        [Required(ErrorMessage = "诊断不能为空")]
        [StringLength(500, ErrorMessage = "诊断长度不能超过500个字符")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>治疗建议</summary>
        [StringLength(1000, ErrorMessage = "治疗建议长度不能超过1000个字符")]
        public string? TreatmentAdvice { get; set; }

        /// <summary>用药医嘱</summary>
        [StringLength(500, ErrorMessage = "用药医嘱长度不能超过500个字符")]
        public string? MedicationInstructions { get; set; }

        /// <summary>生活医嘱</summary>
        [StringLength(500, ErrorMessage = "生活医嘱长度不能超过500个字符")]
        public string? LifestyleInstructions { get; set; }

        /// <summary>复诊建议</summary>
        [StringLength(200, ErrorMessage = "复诊建议长度不能超过200个字符")]
        public string? FollowUpAdvice { get; set; }

        /// <summary>看诊时长（分钟）</summary>
        [Range(1, 300, ErrorMessage = "看诊时长必须在1-300分钟之间")]
        public int Duration { get; set; }

        /// <summary>处方明细</summary>
        public List<PrescriptionItemCreateDto> PrescriptionItems { get; set; } = new();
    }
}