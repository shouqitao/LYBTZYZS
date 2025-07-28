using System.ComponentModel;

namespace LYBT.Models.FormulaTemplates {

    /// <summary>
    /// 经验方模板列表 DTO
    /// </summary>
    public class FormulaTemplateDto {

        /// <summary>模板ID</summary>
        [DisplayName("模板ID")]
        public Guid Id { get; set; }

        /// <summary>模板名称</summary>
        [DisplayName("模板名称")]
        public string Name { get; set; } = string.Empty;
    }
}