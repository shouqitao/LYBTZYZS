using LYBT.Module.Herbs.Dtos;
using System.ComponentModel;

namespace LYBT.Module.FormulaTemplates.Dtos {

    /// <summary>
    /// 经验方模板详情 DTO
    /// </summary>
    public class FormulaTemplateDetailDto {

        /// <summary>模板ID</summary>
        [DisplayName("模板ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>模板名称</summary>
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
        public string? Remark { get; set; }
    }
}
