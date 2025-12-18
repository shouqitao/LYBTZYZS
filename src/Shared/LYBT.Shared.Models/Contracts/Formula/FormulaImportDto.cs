using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 验方导入DTO - 支持从老系统批量导入验方数据
    /// </summary>
    public class FormulaImportDto
    {

        [Required(ErrorMessage = "验方名称不能为空")]
        [StringLength(100, ErrorMessage = "验方名称不能超过100个字符")]
        [DisplayName("验方名称")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "功效描述不能超过200个字符")]
        [DisplayName("功效")]
        public string? Effect { get; set; }

        [StringLength(200, ErrorMessage = "用法描述不能超过200个字符")]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        [StringLength(200, ErrorMessage = "性味归经不能超过200个字符")]
        [DisplayName("性味归经")]
        public string? Property { get; set; }

        [DisplayName("是否共享")]
        public bool IsShared { get; set; } = false;

        [StringLength(500, ErrorMessage = "用药指导不能超过500个字符")]
        [DisplayName("用药指导")]
        public string? Instructions { get; set; }

        [StringLength(500, ErrorMessage = "主治症状不能超过500个字符")]
        [DisplayName("主治症状")]
        public string? Indications { get; set; }

        [StringLength(500, ErrorMessage = "禁忌症不能超过500个字符")]
        [DisplayName("禁忌症")]
        public string? Contraindications { get; set; }

        [StringLength(200, ErrorMessage = "制备方法不能超过200个字符")]
        [DisplayName("制备方法")]
        public string? Preparation { get; set; }

        [StringLength(500, ErrorMessage = "备注不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        [StringLength(200, ErrorMessage = "来源不能超过200个字符")]
        [DisplayName("来源")]
        public string? Source { get; set; }

        [Required(ErrorMessage = "必须包含至少一味中药材")]
        [DisplayName("中药材组成")]
        public List<FormulaHerbImportDto> Herbs { get; set; } = new();

        /// <summary>原系统ID（用于数据迁移）</summary>
        [DisplayName("原系统ID")]
        public string? OriginalId { get; set; }

        /// <summary>导入批次号</summary>
        [DisplayName("导入批次")]
        public string? ImportBatch { get; set; }
    }
}
