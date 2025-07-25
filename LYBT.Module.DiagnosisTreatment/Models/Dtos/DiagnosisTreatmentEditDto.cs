using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.DiagnosisTreatment.Models.Dtos {

    /// <summary>
    /// 编辑诊疗记录 DTO
    /// </summary>
    public class DiagnosisTreatmentEditDto {

        [Required(ErrorMessage = "诊疗ID不能为空")]
        [DisplayName("Id")]
        public Guid Id { get; set; }

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
    }
}