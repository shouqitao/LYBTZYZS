using System.ComponentModel;
namespace LYBT.Module.FormulaTemplates.Dtos {

    /// <summary>
    /// 经验方模板列表 DTO
    /// </summary>
    public class FormulaTemplateDto {

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
    }
}
