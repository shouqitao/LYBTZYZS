using LYBT.Module.Herbs.Dtos;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.FormulaTemplates.Dtos {

    /// <summary>
    /// 编辑经验方模板 DTO
    /// </summary>
    public class FormulaTemplateEditDto {

        /// <summary>模板ID</summary>
        [Required(ErrorMessage = "模板ID不能为空")]
        [DisplayName("模板ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>模板名称</summary>
        [Required(ErrorMessage = "名称不能为空")]
        [DisplayName("模板名称")]
/// <summary>
/// Name 属性。
/// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>药材组成</summary>
        [DisplayName("药材组成")]
/// <summary>
/// Herbs 属性。
/// </summary>
        public List<HerbDto> Herbs { get; set; } = new();

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string? Remark { get; set; } = string.Empty;
    }
}
