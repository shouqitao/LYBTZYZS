using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using LYBT.Models.DiagnosisTreatment;

namespace LYBT.Module.Records.Dtos {
    /// <summary>
    /// 新增病历 DTO
    /// </summary>
    public class RecordCreateDto {
        /// <summary>病人ID</summary>
        [Required(ErrorMessage = "病人ID不能为空")]
        public string PatientId { get; set; } = string.Empty;

        /// <summary>挂号ID</summary>
        [Required(ErrorMessage = "挂号ID不能为空")]
        public Guid RegistrationId { get; set; }

        /// <summary>诊断内容</summary>
        [Required(ErrorMessage = "诊断内容不能为空")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>主诉</summary>
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        public string? PresentIllness { get; set; }

        /// <summary>诊疗建议</summary>
        public string? TreatmentAdvice { get; set; }

        /// <summary>辩证结果列表</summary>
        public List<string> DiagnosisResults { get; set; } = new();

        /// <summary>药材组成</summary>
        public List<HerbItemModel>? HerbalFormula { get; set; }

        /// <summary>辅助治疗方案</summary>
        public List<TreatmentItemModel>? TreatmentPlans { get; set; }

        /// <summary>是否共享</summary>
        public bool IsShared { get; set; }

        /// <summary>共享给医生ID列表</summary>
        public List<string> SharedToDoctorIds { get; set; } = new();

        /// <summary>创建医生ID</summary>
        public string? CreatedBy { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>开方信息（如药方ID）</summary>
        public Guid? PrescriptionId { get; set; }

        /// <summary>病历创建时间</summary>
        public DateTime RecordTime { get; set; } = DateTime.Now;
    }
}
