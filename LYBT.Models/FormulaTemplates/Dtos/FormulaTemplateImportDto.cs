using LYBT.Module.Herbs.Dtos;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.FormulaTemplates.Dtos {
    /// <summary>
    /// 批量导入经验方模板 DTO
    /// </summary>
    public class FormulaTemplateImportDto {
        /// <summary>模板名称</summary>
        [Required(ErrorMessage = "名称不能为空")]
        public string Name { get; set; } = string.Empty;

        /// <summary>药材组成</summary>
        public List<HerbDto> Herbs { get; set; } = new();

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}
