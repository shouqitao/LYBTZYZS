using System.ComponentModel;

namespace LYBT.Models.DiagnosisTreatment {

    /// <summary>
    /// 诊疗详情 DTO
    /// </summary>
    public class DiagnosisTreatmentDetailDto {

        /// <summary>诊疗ID</summary>
        [DisplayName("诊疗ID")]
        public Guid Id { get; set; }

        /// <summary>病人ID</summary>
        [DisplayName("病人ID")]
        public Guid PatientId { get; set; }

        /// <summary>病人姓名</summary>
        [DisplayName("病人姓名")]
        public string PatientName { get; set; } = string.Empty;

        /// <summary>主诉</summary>
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [DisplayName("现病史")]
        public string? PresentIllness { get; set; }

        /// <summary>诊断类型ID</summary>
        [DisplayName("诊断类型ID")]
        public Guid DiagnosisCatalogId { get; set; }

        /// <summary>诊断内容</summary>
        [DisplayName("诊断内容")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>治疗项目</summary>
        [DisplayName("治疗项目")]
        public List<TreatmentItemDto> Treatments { get; set; } = new();

        /// <summary>治疗方</summary>
        [DisplayName("治疗方")]
        public FormulaDto? Formula { get; set; }

        /// <summary>诊疗时间</summary>
        [DisplayName("诊疗时间")]
        public DateTime CreateTime { get; set; }
    }
}