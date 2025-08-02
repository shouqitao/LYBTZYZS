using LYBT.Shared.Models.Contracts.Herbs;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.FormulaTemplates {

    /// <summary>
    /// 经验方模板详情 DTO
    /// </summary>
    public class FormulaTemplateDetailDto {

        /// <summary>模板ID</summary>
        [DisplayName("模板ID")]
        public Guid Id { get; set; }

        /// <summary>模板名称</summary>
        [DisplayName("模板名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>药材组成</summary>
        [DisplayName("药材组成")]
        public List<FormulaIngredientDto> Herbs { get; set; } = new();

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}