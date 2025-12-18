using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 验方中药材导入DTO
    /// </summary>
    public class FormulaHerbImportItemDto
    {

        [Required(ErrorMessage = "中药材名称不能为空")]
        [StringLength(100, ErrorMessage = "中药材名称不能超过100个字符")]
        [DisplayName("中药材名称")]
        public string HerbName { get; set; } = string.Empty;

        [Required(ErrorMessage = "用量必须大于0")]
        [Range(1, 500, ErrorMessage = "用量必须在1-500之间")]
        [DisplayName("用量")]
        public int Dosage { get; set; }

        [StringLength(10, ErrorMessage = "单位不能超过10个字符")]
        [DisplayName("单位")]
        public string Unit { get; set; } = "g";

        [StringLength(50, ErrorMessage = "炮制方法不能超过50个字符")]
        [DisplayName("炮制方法")]
        public string? Preparation { get; set; }

        [StringLength(100, ErrorMessage = "用法不能超过100个字符")]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        [DisplayName("排序")]
        public int SortOrder { get; set; } = 0;

        /// <summary>原系统中药材ID（用于数据迁移）</summary>
        [DisplayName("原系统中药材ID")]
        public string? OriginalHerbId { get; set; }
    }
}
