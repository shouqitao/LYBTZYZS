using LYBT.Models.Herbs;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.FormulaTemplates {

    /// <summary>
    /// 新增经验方模板 DTO
    /// </summary>
    public class FormulaTemplateCreateDto {

        /// <summary>模板名称</summary>
        [Required(ErrorMessage = "名称不能为空")]
        [DisplayName("模板名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>药材组成</summary>
        [DisplayName("药材组成")]
        public List<HerbDto> Herbs { get; set; } = new();

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 经验方模板中的药材明细 DTO
    /// </summary>
}