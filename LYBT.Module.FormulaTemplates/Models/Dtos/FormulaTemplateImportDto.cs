using LYBT.Module.Herbs.Models.Dtos;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.FormulaTemplates.Models.Dtos {

    /// <summary>
    /// 批量导入经验方模板 DTO
    /// </summary>
    public class FormulaTemplateImportDto {

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
}