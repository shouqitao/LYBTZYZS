using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Records.Models.Dtos {

    /// <summary>
    /// 编辑病历 DTO
    /// </summary>
    public class RecordEditDto {

        /// <summary>病历ID</summary>
        [Required(ErrorMessage = "病历ID不能为空")]
        [DisplayName("病历ID")]
        public Guid Id { get; set; }

        /// <summary>诊断内容</summary>
        [Required(ErrorMessage = "诊断内容不能为空")]
        [DisplayName("诊断内容")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>主诉</summary>
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>诊疗建议</summary>
        [DisplayName("诊疗建议")]
        public string? TreatmentAdvice { get; set; }

        /// <summary>开方信息（如药方ID）</summary>
        [DisplayName("开方信息（如药方ID）")]
        public Guid? PrescriptionId { get; set; }

        /// <summary>辩证结果列表</summary>
        [DisplayName("辩证结果列表")]
        public List<string> DiagnosisResults { get; set; } = new();

        /// <summary>药材组成</summary>
        [DisplayName("药材组成")]
        public List<HerbItemModel>? HerbalFormula { get; set; }

        /// <summary>辅助治疗方案</summary>
        [DisplayName("辅助治疗方案")]
        public List<TreatmentItemModel>? TreatmentPlans { get; set; }

        /// <summary>是否共享</summary>
        [DisplayName("是否共享")]
        public bool IsShared { get; set; }

        /// <summary>共享给医生ID列表</summary>
        [DisplayName("共享给医生ID列表")]
        public List<string> SharedToDoctorIds { get; set; } = new();

        /// <summary>病历修改时间</summary>
        [DisplayName("病历修改时间")]
        public DateTime RecordTime { get; set; } = DateTime.Now;
    }
}