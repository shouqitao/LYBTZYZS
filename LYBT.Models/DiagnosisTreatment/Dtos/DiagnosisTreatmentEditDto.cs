using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.DiagnosisTreatment.Models.Dtos {

    /// <summary>
    /// 编辑诊疗记录 DTO
    /// </summary>
    public class DiagnosisTreatmentEditDto {

        [Required(ErrorMessage = "诊疗ID不能为空")]
        [DisplayName("Id")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>主诉</summary>
        [DisplayName("主诉")]
/// <summary>
/// ChiefComplaint 属性。
/// </summary>
        public string? ChiefComplaint { get; set; }

        /// <summary>现病史</summary>
        [DisplayName("现病史")]
/// <summary>
/// PresentIllness 属性。
/// </summary>
        public string? PresentIllness { get; set; }

        /// <summary>诊断类型ID</summary>
        [DisplayName("诊断类型ID")]
/// <summary>
/// DiagnosisCatalogId 属性。
/// </summary>
        public Guid DiagnosisCatalogId { get; set; }

        /// <summary>诊断内容</summary>
        [DisplayName("诊断内容")]
/// <summary>
/// Diagnosis 属性。
/// </summary>
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>治疗项目</summary>
        [DisplayName("治疗项目")]
/// <summary>
/// Treatments 属性。
/// </summary>
        public List<TreatmentItemDto> Treatments { get; set; } = new();

        /// <summary>治疗方</summary>
        [DisplayName("治疗方")]
/// <summary>
/// Formula 属性。
/// </summary>
        public FormulaDto? Formula { get; set; }
    }
}
