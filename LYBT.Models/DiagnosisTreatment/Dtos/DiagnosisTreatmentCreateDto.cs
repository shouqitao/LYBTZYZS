using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.DiagnosisTreatment.Models.Dtos {

    /// <summary>
    /// 新增诊疗记录 DTO
    /// </summary>
    public class DiagnosisTreatmentCreateDto {

        /// <summary>病人ID</summary>
        [Required(ErrorMessage = "病人ID不能为空")]
        [DisplayName("病人ID")]
        public Guid PatientId { get; set; }

        /// <summary>主诉</summary>
        [DisplayName("主诉")]
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史（结构化）</summary>
        [DisplayName("现病史（结构化）")]
        public string? PresentIllness { get; set; }

        /// <summary>诊断类型ID（可配置的诊断目录）</summary>
        [DisplayName("诊断类型ID（可配置的诊断目录）")]
        public Guid DiagnosisCatalogId { get; set; }

        /// <summary>诊断内容</summary>
        [DisplayName("诊断内容")]
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>治疗项目（结构化，列表形式）</summary>
        [DisplayName("治疗项目（结构化，列表形式）")]
        public List<TreatmentItemDto> Treatments { get; set; } = new();

        /// <summary>治疗方（即本次诊疗产生的药方）</summary>
        [DisplayName("治疗方（即本次诊疗产生的药方）")]
        public FormulaDto? Formula { get; set; }
    }
}