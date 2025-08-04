using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Consultation
{
    /// <summary>
    /// 创建看诊记录DTO
    /// </summary>
    public class ConsultationCreateDto
    {
        /// <summary>患者ID</summary>
        [Required(ErrorMessage = "患者ID不能为空")]
        public Guid PatientId { get; set; }

        /// <summary>医生ID</summary>
        [Required(ErrorMessage = "医生ID不能为空")]
        public Guid DoctorId { get; set; }

        /// <summary>科室</summary>
        [Required(ErrorMessage = "科室不能为空")]
        [StringLength(50, ErrorMessage = "科室名称长度不能超过50个字符")]
        public string Department { get; set; } = string.Empty;

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

    /// <summary>
    /// 创建处方项DTO
    /// </summary>
    public class PrescriptionItemCreateDto
    {
        /// <summary>药材ID</summary>
        [Required(ErrorMessage = "药材ID不能为空")]
        public Guid HerbId { get; set; }

        /// <summary>用量</summary>
        [Required(ErrorMessage = "用量不能为空")]
        [Range(0.01, 9999.99, ErrorMessage = "用量必须在0.01-9999.99之间")]
        public decimal Dosage { get; set; }

        /// <summary>单位</summary>
        [Required(ErrorMessage = "单位不能为空")]
        [StringLength(10, ErrorMessage = "单位长度不能超过10个字符")]
        public string Unit { get; set; } = string.Empty;

        /// <summary>用法</summary>
        [StringLength(50, ErrorMessage = "用法长度不能超过50个字符")]
        public string? Usage { get; set; }
    }
}