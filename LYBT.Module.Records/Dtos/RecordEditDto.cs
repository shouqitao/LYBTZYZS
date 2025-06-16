using System;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Records.Dtos {
    /// <summary>
    /// 编辑病历 DTO
    /// </summary>
    public class RecordEditDto {
        /// <summary>病历ID</summary>
        [Required(ErrorMessage = "病历ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>诊断内容</summary>
        [Required(ErrorMessage = "诊断内容不能为空")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>主诉</summary>
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        public string? PresentIllness { get; set; }

        /// <summary>诊疗建议</summary>
        public string? TreatmentAdvice { get; set; }

        /// <summary>开方信息（如药方ID）</summary>
        public Guid? PrescriptionId { get; set; }

        /// <summary>病历修改时间</summary>
        public DateTime RecordTime { get; set; } = DateTime.Now;
    }
}
